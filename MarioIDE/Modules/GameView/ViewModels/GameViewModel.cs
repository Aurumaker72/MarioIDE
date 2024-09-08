using Caliburn.Micro;
using Gemini.Framework.Services;
using ImGuiNET;
using MarioIDE.Core.Framework;
using MarioIDE.Core.Logic;
using MarioIDE.Core.Modules.Timeline;
using MarioIDE.Framework;
using MarioIDE.Framework.ViewModels;
using MarioIDE.Logic;
using MarioSharp;
using MarioSharp.Structs;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;
using Vector3 = OpenTK.Vector3;

namespace MarioIDE.Modules.GameView.ViewModels;

[Export(typeof(IGameView))]
internal class GameViewModel : GlToolViewModel, IGameView
{
    public override PaneLocation PreferredLocation => PaneLocation.Left;
    public override string DisplayName => "Game View";
    public int CameraYaw { get; private set; }

    private readonly GlState _defaultGlState;
    private readonly int _frameBuffer;
    private readonly int _renderBuffer;
    private readonly int _textureId;

    private Size _gameResolution = new(1280, 960);
    private int _selectedCameraModeIndex;

    private float _lastX;
    private float _lastY;
    private float _cameraSpeed;

    private float _currentPitch;
    private float _currentYaw;

    private float _targetPitch;
    private float _targetYaw;

    private float _targetZoom;
    private float _currentZoom;

    private Vector3 _freeCamTargetPos;
    private Vector3 _freeCamCurrentPos;

    private bool _orthographic;
    private bool _hideHud;

    public GameViewModel()
    {
        _defaultGlState = GlHelpers.SaveGlState();

        // generate / bind framebuffer
        GL.GenFramebuffers(1, out _frameBuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

        // generate texture
        GL.GenTextures(1, out _textureId);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _gameResolution.Width, _gameResolution.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 0x2601);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 0x2601);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        // attach it to currently bound framebuffer object
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureId, 0);

        // generate renderbuffer
        GL.GenRenderbuffers(1, out _renderBuffer);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, _gameResolution.Width, _gameResolution.Height);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        // bind renderbuffer
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _renderBuffer);

        // unbind framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public override void OnRender()
    {
        IProject activeTimeline = (IProject)IoC.Get<IShell>().ActiveItem;
        if (activeTimeline?.SaveSystem?.GameInstance != null)
        {
            SaveSystem saveSystem = (SaveSystem)activeTimeline.SaveSystem;
            GameInstance gameInstance = saveSystem.GameInstance;

            //calculate the size to render the game at to fit the view while respecting aspect ratio
            const float aspectRatio = 4.0f / 3.0f;
            float ratio = Math.Min(Width / aspectRatio, Height);
            int newWidth = (int)(aspectRatio * ratio);
            int newHeight = (int)ratio;

            _gameResolution.Width = Math.Max(1, newWidth);
            _gameResolution.Height = Math.Max(1, newHeight);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, _gameResolution.Width, _gameResolution.Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _gameResolution.Width, _gameResolution.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.Viewport(0, 0, _gameResolution.Width, _gameResolution.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color.Black);

            gameInstance.Load(saveSystem.PreviousFrame.IsValid() ? saveSystem.PreviousFrame : saveSystem.PowerOn);
            gameInstance.SetGfxDimensions(_gameResolution.Width, _gameResolution.Height);

            MarioState marioState = gameInstance.Memory.GMarioStates;
            OverrideCamera overrideCamera = gameInstance.Memory.GOverrideCamera;

            float deltaX = _lastX - ImGui.GetIO().MousePos.X;
            float deltaY = _lastY - ImGui.GetIO().MousePos.Y;

            if (MouseState.RightButton)
            {
                _targetPitch += deltaY * 0.004f;
                _targetYaw += deltaX * 0.004f;
            }

            if (_targetPitch > 1.57)
            {
                _targetPitch = 1.57f;
            }

            if (_targetPitch < -1.57)
            {
                _targetPitch = -1.57f;
            }

            _currentPitch = MathUtils.Lerp(_currentPitch, _targetPitch, Time.DeltaTime * 25.0f);
            _currentYaw = MathUtils.Lerp(_currentYaw, _targetYaw, Time.DeltaTime * 25.0f);

            _lastX = ImGui.GetIO().MousePos.X;
            _lastY = ImGui.GetIO().MousePos.Y;

            _targetZoom -= ImGui.GetIO().MouseWheel / 100f;
            _targetZoom = MathHelper.Clamp(_targetZoom, 0.01f, 1f);
            _currentZoom = MathUtils.Lerp(_currentZoom, _targetZoom * 100f, Time.DeltaTime * 8.0f);

            Vector3 target = marioState.pos;

            int fov = 90;
            float targetSize = (1280 * _currentZoom + 960 * _currentZoom) / 2;
            double distance = targetSize / Math.Tan(fov / 2f);

            double offset = distance;

            if (_orthographic)
            {
                offset = 65535;
            }

            Vector3 faceDirection = MathUtils.AngleToDirection(_currentPitch, _currentYaw);

            overrideCamera.enabled = _selectedCameraModeIndex != 0;

            if (_selectedCameraModeIndex == 0 || _selectedCameraModeIndex == 1) // Orbit Camera
            {
                _freeCamTargetPos.X = (float)(target.X - offset * faceDirection.X);
                _freeCamTargetPos.Y = (float)(target.Y - offset * faceDirection.Y);
                _freeCamTargetPos.Z = (float)(target.Z - offset * faceDirection.Z);
                overrideCamera.pos = _freeCamCurrentPos = _freeCamTargetPos;
                overrideCamera.focus = target;
            }
            else if (_selectedCameraModeIndex == 2) // Free Camera
            {
                Vector3 right = Vector3.Cross(Vector3.Normalize(new Vector3(faceDirection.X, faceDirection.Y, faceDirection.Z)), new Vector3(0, 1, 0));
                right = Vector3.Normalize(right);

                Vector3 up = Vector3.Cross(Vector3.Normalize(new Vector3(faceDirection.X, faceDirection.Y, faceDirection.Z)), right);
                up = Vector3.Normalize(up);

                float targetSpeed = 0;
                float targetSpeedMoving = 500f * (KeyboardState.IsKeyDown(Keys.ShiftKey) ? 10f : 1f);

                if (KeyboardState.IsKeyDown(Keys.W))
                {
                    _freeCamTargetPos.X += _cameraSpeed * faceDirection.X * Time.DeltaTime;
                    _freeCamTargetPos.Y += _cameraSpeed * faceDirection.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z += _cameraSpeed * faceDirection.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                if (KeyboardState.IsKeyDown(Keys.S))
                {
                    _freeCamTargetPos.X -= _cameraSpeed * faceDirection.X * Time.DeltaTime;
                    _freeCamTargetPos.Y -= _cameraSpeed * faceDirection.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z -= _cameraSpeed * faceDirection.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                if (KeyboardState.IsKeyDown(Keys.A))
                {
                    _freeCamTargetPos.X -= _cameraSpeed * right.X * Time.DeltaTime;
                    _freeCamTargetPos.Y -= _cameraSpeed * right.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z -= _cameraSpeed * right.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                if (KeyboardState.IsKeyDown(Keys.D))
                {
                    _freeCamTargetPos.X += _cameraSpeed * right.X * Time.DeltaTime;
                    _freeCamTargetPos.Y += _cameraSpeed * right.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z += _cameraSpeed * right.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                if (KeyboardState.IsKeyDown(Keys.ControlKey))
                {
                    _freeCamTargetPos.X += _cameraSpeed * up.X * Time.DeltaTime;
                    _freeCamTargetPos.Y += _cameraSpeed * up.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z += _cameraSpeed * up.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                if (KeyboardState.IsKeyDown(Keys.Space))
                {
                    _freeCamTargetPos.X -= _cameraSpeed * up.X * Time.DeltaTime;
                    _freeCamTargetPos.Y -= _cameraSpeed * up.Y * Time.DeltaTime;
                    _freeCamTargetPos.Z -= _cameraSpeed * up.Z * Time.DeltaTime;
                    targetSpeed = targetSpeedMoving;
                }

                _cameraSpeed = MathUtils.Lerp(_cameraSpeed, targetSpeed, Time.DeltaTime * 2.0f);
                Vector3 currentPos = new Vector3(_freeCamCurrentPos.X, _freeCamCurrentPos.Y, _freeCamCurrentPos.Z);
                Vector3 targetPos = new Vector3(_freeCamTargetPos.X, _freeCamTargetPos.Y, _freeCamTargetPos.Z);
                Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.DeltaTime * 10.0f);
                _freeCamCurrentPos = overrideCamera.pos = new Vector3(newPos.X, newPos.Y, newPos.Z);
                overrideCamera.focus = new Vector3(newPos.X + faceDirection.X, newPos.Y + faceDirection.Y, newPos.Z + faceDirection.Z);
            }

            //gameInstance.Memory.GOverrideMatrix = _orthographic;
            gameInstance.Memory.GOverridePerspectiveMatrix = _orthographic;

            if (_orthographic)
            {
                //overrideCamera.pos = new Vector3(marioState.pos.X + 0.001f, marioState.pos.Y + 65535, marioState.pos.Z);
                //overrideCamera.focus = marioState.pos;

                //gameInstance.Memory.GMatrixOverride = ConvertPerspectiveToOrthographic(Matrix4.LookAt(overrideCamera.pos, overrideCamera.focus, new Vector3(0, 1, 0)).ToMatrix4X4(), 0.01f, 65535 * 32f);
                gameInstance.Memory.GMatrixPerspectiveOverride = Matrix4x4.CreateOrthographic(1280 * _currentZoom, 960 * _currentZoom, 0.01f, 65535 * 32);
                //gameInstance.Memory.GMatrixOverride = Matrix4.LookAt(overrideCamera.pos, overrideCamera.focus, new Vector3(0, 1, 0)).ToMatrix4X4();
            }

            overrideCamera.roll = 0;
            gameInstance.Memory.GOverrideCamera = overrideCamera;
            gameInstance.Memory.GHideHud = _hideHud;

            //TODO: toggle in gui
            gameInstance.Memory.GOverrideFarClip = 65535 * 32;
            gameInstance.Memory.GOverrideCulling = true;

            if (gameInstance.Memory.GOverrideCamera.enabled)
            {
                CameraYaw = InGameAngleTo(saveSystem, overrideCamera.focus.X, overrideCamera.focus.Z, overrideCamera.pos.X, overrideCamera.pos.Z);
            }
            else
            {
                CameraYaw = gameInstance.Memory.GetCameraYaw();
            }

            // Restore the GL state, bind the framebuffer, and perform a game update. This setup enables rendering the game into a texture that can be seamlessly displayed on the GUI.
            GlHelpers.RestoreGlState(_defaultGlState);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);
            gameInstance.Update(saveSystem.InputManager.GetFrameInput(gameInstance.Frame), true, true);

            // Render within the game world in 3D space.
            //Render3dSpace(saveSystem);

            // Restore the GL state, unbind framebuffer and set the viewport for ImGui.
            GlHelpers.RestoreGlState(_defaultGlState);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Width, Height);

            // Finally, render on the view, using ImGui. (We are rendering the game from texture and the top menu inside this method)
            ImGuiRender(saveSystem);

            // Reload the frame to get rid of any change we could have made to memory. Prevent timeline 'desync'
            gameInstance.Load(saveSystem.CurrentFrame.IsValid() ? saveSystem.CurrentFrame : saveSystem.PowerOn);
        }
    }

    /// <summary>
    /// Use the ingame atan function to return angle between two points
    /// </summary>
    //TODO: move this out of here
    private static ushort InGameAngleTo(ISaveSystem saveSystem, float xFrom, float zFrom, float xTo, float zTo)
    {
        return saveSystem.GameInstance.Memory.ATan(zTo - zFrom, xTo - xFrom);
    }

    private void Render3dSpace(SaveSystem saveSystem)
    {
        // Restore the GL state
        GlHelpers.RestoreGlState(_defaultGlState);

        // Update the GL state with matrices from the game's memory to align our rendering perspective with the game.
        // This ensures that everything we draw appears as if it belongs in the game's world.
        GameInstance gameInstance = saveSystem.GameInstance;
        Matrix4 perpective = gameInstance.Memory.GMatrixPerspective.ToMatrix4();
        Matrix4 lookat = gameInstance.Memory.GMatrix.ToMatrix4();

        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref perpective);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadMatrix(ref lookat);

        // Enabling depth test ensures proper occlusion and prevents rendering over existing elements.
        GL.Enable(EnableCap.DepthTest);

        // Enable face culling to optimize rendering.
        GL.Enable(EnableCap.CullFace);

        //TODO: register 3d renderers from adddons, restore gl state for each implementations...

        //Render the movement path
        //TODO: move this in an addon. toggle and length in gui
        RenderMovementPath(Color4.Red, saveSystem, 1200);
    }

    /// <summary>
    /// Render the game to the view using a imgui texture
    /// </summary>
    private void ImGuiRender(ISaveSystem saveSystem)
    {
        //clip imgui to the view size
        ImGui.PushClipRect(Vector2.Zero, new Vector2(Width, Height), false);

        //calculate the size to render the game at to fit the view while respecting aspect ratio
        const float aspectRatio = 4.0f / 3.0f;
        float height = Math.Min(Width / aspectRatio, Height);
        int newWidth = (int)(aspectRatio * height);
        int newHeight = (int)height;

        //render the game from a texture using imgui
        ImGui.GetWindowDrawList().AddImage((IntPtr)_textureId,
            new Vector2((Width - newWidth) / 2f, 32 + (Height - newHeight) / 2f),
            new Vector2(newWidth + (Width - newWidth) / 2f, newHeight + (Height - newHeight) / 2f),
            new Vector2(0, 1),
            new Vector2(1, 0));

        //draw the top menu bar
        ImGui.GetWindowDrawList().AddRectFilled(new Vector2(0, 0), new Vector2(Width, 32), ImGuiUtils.Color(40, 42, 44, 255));

        //camera mode combo
        ImGui.Text("Camera Mode");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.Combo("##CameraMode", ref _selectedCameraModeIndex, new[] { "Lakitu", "Orbit", "Free" }, 3);

        //orthographic toggle
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        ImGui.Text("Orthographic");
        ImGui.SameLine();
        ImGui.Checkbox("##Orthographic", ref _orthographic);

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("Side"))
        {
            _targetPitch = 0;
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("Top"))
        {
            _targetPitch = -1.57f; // -89.95437
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("Bottom"))
        {
            _targetPitch = 1.57f; // 89.95437
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("North"))
        {
            _targetYaw = 3.14159f; // 180
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("South"))
        {
            _targetYaw = 0;
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("West"))
        {
            _targetYaw = 4.71239f; // 270
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        if (ImGui.Button("East"))
        {
            _targetYaw = 1.5708f; // 90
        }

        //hide HUD toggle
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        ImGui.Text("Hide HUD");
        ImGui.SameLine();
        ImGui.Checkbox("##Hide HUD", ref _hideHud);

        //pitch label
        ImGui.SameLine(Width - 380);
        ImGui.Text("Pitch: " + _currentPitch.ToString("0.00"));

        //yaw label
        ImGui.SameLine(Width - 290);
        ImGui.Text("Yaw: " + _currentYaw.ToString("0.00"));

        //zoom label
        ImGui.SameLine(Width - 210);
        ImGui.Text("Zoom: " + _currentZoom.ToString("0.00"));

        //time label
        //TODO: make this more accurate???
        const float secondsPerFrame = 1.0f / 30.0f;
        string timeText = TimeSpan.FromSeconds(saveSystem.CurrentFrame.Frame * secondsPerFrame).ToString("hh\\:mm\\:ss\\.ff");
        ImGui.SameLine(Width - 128);
        ImGui.Text("Time: " + timeText);

        //stop clipping
        ImGui.PopClipRect();
    }

    //TODO: MOVE EVERYTHING BELOW IN AN ADDON 

    private static void RenderMovementPath(Color4 color, SaveSystem saveSystem, int length)
    {
        //Get the frame data for the current frame
        FrameData lastFrameData = saveSystem.GetFrameData(saveSystem.CurrentFrame.Frame);

        for (int i = 1; i < length; i++)
        {
            //Get the frame data for the current frame + 'i'
            FrameData frameData = saveSystem.GetFrameData(saveSystem.CurrentFrame.Frame + i);

            //If we changed level, stop rendering the path, exit the loop
            if (frameData.Level != lastFrameData.Level || frameData.Area != lastFrameData.Area)
                break;

            //Render the line segment
            RenderMovementPathLineSegment(color, lastFrameData.MarioPos, frameData.MarioPos);

            //at last, update lastFrameData
            lastFrameData = frameData;
        }
    }

    private static void RenderMovementPathLineSegment(Color4 color, Vector3 startPos, Vector3 endPos)
    {
        //set the rendering color
        GL.Color4(color);

        //draw a line from startPos to endPos
        GL.Begin(PrimitiveType.Lines);
        GL.Vertex3(startPos);
        GL.Vertex3(endPos);
        GL.End();

        //draw a big point at startPos
        GL.PointSize(8);
        GL.Begin(PrimitiveType.Points);
        GL.Vertex3(startPos);
        GL.End();

        //draw a small point for every quarter frame
        GL.PointSize(4);
        GL.Begin(PrimitiveType.Points);
        GL.Vertex3(Vector3.Lerp(startPos, endPos, 0.25f));
        GL.Vertex3(Vector3.Lerp(startPos, endPos, 0.5f));
        GL.Vertex3(Vector3.Lerp(startPos, endPos, 0.75f));
        GL.End();
    }
}