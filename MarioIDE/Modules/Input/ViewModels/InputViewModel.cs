using Caliburn.Micro;
using Gemini.Framework.Services;
using ImGuiNET;
using MarioIDE.Core.Enums;
using MarioIDE.Core.Modules.Timeline;
using MarioIDE.Framework.Models;
using MarioIDE.Framework.ViewModels;
using MarioIDE.Logic;
using MarioIDE.Modules.GameView.ViewModels;
using MarioSharp;
using MarioSharp.Structs;
using MarioSharp.Structs.Input;
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MarioIDE.Modules.Input.ViewModels;

[Export(typeof(IInput))]
public class InputViewModel : GlToolViewModel, IInput
{
    public override PaneLocation PreferredLocation => PaneLocation.Left;
    public override string DisplayName => "Input";

    private readonly ImGuiJoystick _joystick;
    private const float JoystickSize = 128 * 1.5f;
    private readonly Vector2 _joystickTopLeft = new(16, 16);

    public InputViewModel()
    {
        _joystick = new ImGuiJoystick(_joystickTopLeft, JoystickSize);
    }

    public override void OnRender()
    {
        IProject activeTimeline = (IProject)IoC.Get<IShell>().ActiveItem;
        if (activeTimeline?.SaveSystem?.GameInstance != null)
        {
            SaveSystem saveSystem = (SaveSystem)activeTimeline.SaveSystem;
            DrawWindow(activeTimeline, saveSystem);
        }
    }

    private void DrawWindow(IProject timeline, SaveSystem saveSystem)
    {
        GameInstance gameInstance = saveSystem.GameInstance;
        //gameInstance.Load(saveSystem.CurrentFrame);

        GameMemory memory = gameInstance.Memory;
        MarioState marioState = memory.GMarioStates;
        int cameraYaw = memory.GetCameraYaw();
        int gameViewCameraYaw = IoC.Get<IGameView>().CameraYaw;

        int frame = timeline.SelectionStart;
        OsContPad originalInput = saveSystem.InputManager.GetFrameInput(frame);
        OsContPad input = originalInput;

        /*AdjustedStick adjusted = InputUtils.StickRawToAdjusted((short)input.X, (short)input.Y);
        IntendedStick intended = InputUtils.StickAdjustedToIntended(adjusted, marioState.faceAngle.Y, (short)cameraYaw, marioState.squishTimer != 0);
        Point relativeStick = InputUtils.IntendedToRaw(marioState.faceAngle.Y, gameViewCameraYaw, marioState.squishTimer, intended.Yaw, intended.Mag, 0);

        int inputX = relativeStick.X;
        int inputY = relativeStick.Y;
        _joystick.Draw(KeyboardState, ref inputX, ref inputY);

        adjusted = InputUtils.StickRawToAdjusted((short)inputX, (short)inputY);
        intended = InputUtils.StickAdjustedToIntended(adjusted, marioState.faceAngle.Y, (short)gameViewCameraYaw, marioState.squishTimer != 0);
        relativeStick = InputUtils.IntendedToRaw(marioState.faceAngle.Y, (short)cameraYaw, marioState.squishTimer, intended.Yaw, intended.Mag, 0);

        inputX = relativeStick.X;
        inputY = relativeStick.Y;

        input.X = inputX;
        input.Y = inputY;*/

        int inputX = input.X;
        int inputY = input.Y;
        _joystick.Draw(KeyboardState, ref inputX, ref inputY);
        input.X = inputX;
        input.Y = inputY;

        ImGui.Dummy(_joystickTopLeft + new Vector2(JoystickSize + 8, JoystickSize));
        ImGui.SameLine();
        ImGui.BeginGroup();

        bool forceChange = false;

        ImGui.Dummy(new Vector2(0, 8));
        if (ImGui.Button("Zero"))
        {
            input.X = 0;
            input.Y = 0;
            forceChange = true;
        }

        ImGui.Dummy(new Vector2(0, 2));
        if (ImGui.Button("SpeedKick"))
        {
            float magnitude = new Vector2(inputX, inputY).Length();
            input.X = inputX == 0 || magnitude == 0 ? 0 : (int)(inputX / magnitude * 47);
            input.Y = inputY == 0 || magnitude == 0 ? 0 : (int)(inputY / magnitude * 47);
            forceChange = true;
        }

        ImGui.Dummy(new Vector2(0, 2));
        if (ImGui.Button("Match Angle"))
        {
            for (int i = timeline.SelectionStart; i <= timeline.SelectionEnd; i++)
            {
                OsContPad frameInput = saveSystem.InputManager.GetFrameInput(i);
                FrameData frameData = saveSystem.GetFrameData(i);
                Point newInput = GetInputsForAngle(GetEffectiveAngle(frameData.FaceAngle.Y), frameData.CameraYaw);
                frameInput.X = newInput.X;
                frameInput.Y = newInput.Y;
                saveSystem.InputManager.SetFrameInput(i, frameInput);
            }

            //((SaveSystem)_tasEdit.Project.SaveSystem).InvalidateFrames(timeline.SelectionStart);
        }

        ImGui.EndGroup();

        if (!originalInput.Equals(input) || forceChange)
        {
            for (int i = timeline.SelectionStart; i <= timeline.SelectionEnd; i++)
            {
                input.Buttons = saveSystem.InputManager.GetFrameInput(i).Buttons;
                saveSystem.InputManager.SetFrameInput(i, input, false);
            }

            saveSystem.InvalidateFrames(timeline.SelectionStart);
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(8, 0));
        ImGui.SameLine();
        ImGui.BeginGroup();

        ImGui.Text("Facing Yaw: " + marioState.faceAngle.Y);
        ImGui.Text("Intended Yaw: " + marioState.intendedYaw);
        ImGui.Text("Camera Yaw: " + cameraYaw);
        ImGui.Text("Game View Camera Yaw: " + gameViewCameraYaw);
        ImGui.Text("Inputs: " + GetInputsForAngle(GetEffectiveAngle(marioState.faceAngle.Y), cameraYaw));

        ImGui.EndGroup();
    }

    private static int GetEffectiveAngle(int angle)
    {
        return angle - angle % 16;
    }

    private static Point GetInputsForAngle(int goal, int cameraYaw)
    {
        goal -= 65536;

        while (GetEffectiveAngle(cameraYaw) > goal)
        {
            goal += 65536;
        }

        int minang = 0;
        int maxang = Angles.Array.Length - 1;
        int midang = (int)Math.Floor(minang + maxang / 2f);

        while (minang <= maxang)
        {
            int effectiveAngle = GetEffectiveAngle(Angles.Array[midang].Key + cameraYaw);
            if (effectiveAngle < goal)
            {
                minang = midang + 1;
            }
            else if (effectiveAngle == goal)
            {
                minang = midang;
                maxang = midang - 1;
            }
            else
            {
                maxang = midang - 1;
            }

            midang = (int)Math.Floor((minang + maxang) / 2f);
        }

        if (minang > Angles.Array.Length - 1)
        {
            minang = 0;
            if (Math.Abs(GetEffectiveAngle(Angles.Array[0].Key + cameraYaw) - (goal - 65536)) > Math.Abs(GetEffectiveAngle(Angles.Array[Angles.Array.Length - 1].Key + cameraYaw) - goal))
            {
                minang = Angles.Array.Length - 1;
            }
        }

        return Angles.Array[minang].Value;
    }

    private class ImGuiJoystick
    {
        private bool _isDragging;

        private readonly Vector2 _topLeft;
        private Vector2 _lastMousePos;

        private const int Range = 128;
        private readonly float _scale;
        private readonly float _size;

        private Vector2 _stickPosition = Vector2.Zero;

        public ImGuiJoystick(Vector2 topLeft, float size)
        {
            _scale = size / Range;
            _topLeft = topLeft;
            _size = size;
        }

        public void Draw(KeyboardState keyboardState, ref int x, ref int y)
        {
            Vector2 size = new Vector2(_size, _size);
            Vector2 bottomRight = _topLeft + size;

            Vector2 mousePos = ImGui.GetIO().MousePos;

            if (!ImGui.GetIO().MouseDown[0])
            {
                _isDragging = false;
                SetStickPosition(x, y);
                ClampStickPosition();
            }
            else if (!_isDragging && Vector2.Distance(mousePos, _stickPosition) < 8 * _scale)
            {
                _isDragging = true;
                _lastMousePos = mousePos;
            }

            if (_isDragging)
            {
                _stickPosition += (mousePos - _lastMousePos) / (keyboardState.IsKeyDown(Keys.LShiftKey) ? 8f : 1f);
                ClampStickPosition();
                Vector2 newStickPosition = GetStickPosition();
                x = (int)Math.Round(newStickPosition.X);
                y = (int)Math.Round(newStickPosition.Y);
                _lastMousePos = mousePos;
            }

            ImGui.GetWindowDrawList().AddRect(_topLeft, bottomRight, 0xFF000000);
            ImGui.GetWindowDrawList().AddCircleFilled(_stickPosition, 8 * _scale, 0xFF000000);
        }

        private void SetStickPosition(int x, int y)
        {
            _stickPosition = _topLeft + new Vector2(_size, _size) / 2f + new Vector2(x * _scale / 2f, -y * _scale / 2f);
        }

        private Vector2 GetStickPosition()
        {
            Vector2 result = (_stickPosition - (_topLeft + new Vector2(_size, _size) / 2f)) / _scale * 2f;
            result.Y = -result.Y;

            if (result.X > 127)
                result.X = 127;
            if (result.Y > 127)
                result.Y = 127;
            if (result.X < -128)
                result.X = -128;
            if (result.Y < -128)
                result.Y = -128;

            return result;
        }

        private void ClampStickPosition()
        {
            _stickPosition.X = Math.Min(Math.Max(_stickPosition.X, _topLeft.X), _topLeft.X + Range * _scale);
            _stickPosition.Y = Math.Min(Math.Max(_stickPosition.Y, _topLeft.Y), _topLeft.Y + Range * _scale);
        }
    }
}