using Caliburn.Micro;
using Gemini.Framework.Services;
using ImGuiNET;
using MarioIDE.Core.Modules.Timeline;
using MarioIDE.Framework;
using MarioIDE.Framework.ViewModels;
using MarioIDE.Logic;
using MarioSharp;
using MarioSharp.Structs;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace MarioIDE.Modules.SceneView.ViewModels;

[Export(typeof(ISceneView))]
internal class SceneViewModel : GlToolViewModel, ISceneView
{
    public override PaneLocation PreferredLocation => PaneLocation.Left;
    public override string DisplayName => "Scene View";

    private readonly int _vertexBufferObject;
    private readonly int _vertexArrayObject;

    private readonly Shader _lightingShader;
    private readonly Texture _diffuseMap;

    private readonly float[] _vertices =
    {
        -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, // Bottom-left vertex
        0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,// Bottom-right vertex
        0.0f,  0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, // Top vertex
    };

    private int _selectedCameraModeIndex;

    public SceneViewModel()
    {
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StreamDraw);

        _lightingShader = new Shader("shaders/shader.vert", "shaders/lighting.frag");

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        int positionLocation = _lightingShader.GetAttribLocation("aPos");
        GL.EnableVertexAttribArray(positionLocation);
        GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

        int normalLocation = _lightingShader.GetAttribLocation("aNormal");
        GL.EnableVertexAttribArray(normalLocation);
        GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

        int texCoordLocation = _lightingShader.GetAttribLocation("aTexCoords");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

        _diffuseMap = Texture.LoadFromFile("resources/triangle.png");
    }

    public override void OnRender()
    {
        IProject activeTimeline = (IProject)IoC.Get<IShell>().ActiveItem;
        if (activeTimeline?.SaveSystem?.GameInstance != null)
        {
            SaveSystem saveSystem = (SaveSystem)activeTimeline.SaveSystem;
            GameInstance gameInstance = saveSystem.GameInstance;
            //gameInstance.Load(saveSystem.CurrentFrame);

            //GlHelpers.RestoreGlState(Module.DefaultGlState);

            GL.Enable(EnableCap.ScissorTest);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.ScissorTest);

            //GL.ClearColor(Color4.Black);
            GL.Viewport(0, 0, Width, Height);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.MatrixMode(MatrixMode.Projection);
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60.0f), Width / (float)Height, 1f, 100000.0f);
            GL.LoadMatrix(ref perpective);

            GL.MatrixMode(MatrixMode.Modelview);
            Matrix4 lookat = Matrix4.LookAt(gameInstance.Memory.GLakituState.CurPos, gameInstance.Memory.GLakituState.CurFocus, new Vector3(0, 1, 0));
            GL.LoadMatrix(ref lookat);

            GL.BindVertexArray(_vertexArrayObject);

            _diffuseMap.Use(TextureUnit.Texture0);
            _lightingShader.Use();

            _lightingShader.SetMatrix4("model", Matrix4.Identity);
            _lightingShader.SetMatrix4("view", lookat);
            _lightingShader.SetMatrix4("projection", perpective);

            Quaternion sunRotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(35), MathHelper.DegreesToRadians(0), MathHelper.DegreesToRadians(25));
            Vector3 sunDirection = sunRotation * new Vector3(0, 1, 0);

            Quaternion sunRotation2 = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(35), MathHelper.DegreesToRadians(180), MathHelper.DegreesToRadians(25));
            Vector3 sunDirection2 = sunRotation2 * new Vector3(0, 1, 0);

            Vector3 ceilingColor = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 floorColor = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 wallColor = new Vector3(0.0f, 0.0f, 1.0f);

            _lightingShader.SetVector3("lightColor", new Vector3(0.5f, 1.0f, 0.5f));
            _lightingShader.SetVector3("lightColor2", new Vector3(0.5f, 0.5f, 1.0f));
            _lightingShader.SetVector3("lightDir", sunDirection);
            _lightingShader.SetVector3("lightDir2", sunDirection2);

            GameMemory memory = gameInstance.Memory;
            IntPtr surfacePool = memory.SSurfacePool;
            int surfaceCount = memory.GSurfacesAllocated;

            for (int i = 0; i < surfaceCount; i++)
            {
                IntPtr surfacePtr = surfacePool + i * Marshal.SizeOf<Surface>();
                Surface surface = gameInstance.Read<Surface>(surfacePtr);

                Vector3 surfaceColor = wallColor;
                SurfaceType surfaceType = GetSurfaceType(surface);

                if (surfaceType == SurfaceType.Ceiling)
                {
                    surfaceColor = ceilingColor;
                }
                else if (surfaceType == SurfaceType.Floor)
                {
                    surfaceColor = floorColor;
                }
                else if (surfaceType == SurfaceType.Wall)
                {
                    surfaceColor = wallColor;
                }

                _lightingShader.SetVector3("objectColor", surfaceColor);

                float[] vertices =
                {
                    surface.vertex1.X, surface.vertex1.Y, surface.vertex1.Z, surface.normal.X, surface.normal.Y, surface.normal.Z, 0, 1,
                    surface.vertex2.X, surface.vertex2.Y, surface.vertex2.Z, surface.normal.X, surface.normal.Y, surface.normal.Z, 0, 0,
                    surface.vertex3.X, surface.vertex3.Y, surface.vertex3.Z, surface.normal.X, surface.normal.Y, surface.normal.Z, 1, 1
                };

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            }

            /*GL.UseProgram(0);
            GL.Disable(EnableCap.CullFace);

            FrameData fd = saveSystem.GetFrameData(saveSystem.GetCurrentFrame().Frame);

            for (int i = 0; i <= 120; i++)
            {
                FrameData frameData = saveSystem.GetFrameData(saveSystem.GetCurrentFrame().Frame + i);
                if (i <= 0 && (frameData.Level != fd.Level || frameData.Area != fd.Area))
                {
                    continue;
                }

                if (i > 0 && (frameData.Level != fd.Level || frameData.Area != fd.Area))
                {
                    break;
                }

                Vector3 pos = frameData.MarioPos;
                Vector3 nextPos = saveSystem.GetFrameData(saveSystem.GetCurrentFrame().Frame + i + 1).MarioPos;

                GL.Color4(Color4.White);

                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(pos);
                GL.Vertex3(nextPos);
                GL.End();

                GL.PointSize(4);
                GL.Begin(PrimitiveType.Points);
                GL.Vertex3(pos);
                GL.End();

                GL.Color4(Color4.Red);

                GL.PointSize(2);
                GL.Begin(PrimitiveType.Points);
                GL.Vertex3(Vector3.Lerp(pos, nextPos, 0.25f));
                GL.Vertex3(Vector3.Lerp(pos, nextPos, 0.50f));
                GL.Vertex3(Vector3.Lerp(pos, nextPos, 0.75f));
                GL.End();
            }*/

            //GlHelpers.RestoreGlState(Module.DefaultGlState);
            RenderGui();
        }
    }

    private void RenderGui()
    {
        ImGui.PushClipRect(new Vector2(0, 0), new Vector2(Width, Height), false);
        ImGui.GetWindowDrawList().AddRectFilled(new Vector2(0, 0), new Vector2(Width, 32), ImGuiUtils.Color(60, 63, 65, 255));
        ImGui.PopClipRect();

        ImGui.Text("Camera Mode");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.Combo("##CameraMode", ref _selectedCameraModeIndex, new[] { "Lakitu", "Orbit", "Free" }, 3);
    }

    private static SurfaceType GetSurfaceType(Surface surface)
    {
        if (surface.normal.Y > 0.01)
        {
            return SurfaceType.Floor;
        }
        if (surface.normal.Y < -0.01)
        {
            return SurfaceType.Ceiling;
        }
        return SurfaceType.Wall;
    }


    public enum SurfaceType
    {
        Floor,
        Ceiling,
        Wall
    }
}