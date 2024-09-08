using MarioIDE.Core.Logic;
using MarioIDE.Mupen;
using MarioSharp.Structs.Input;
using System.Collections.Generic;

namespace MarioIDE.Logic;

internal class InputManager : IInputManager
{
    private readonly List<InputModel> _inputs = new();
    private readonly SaveSystem _saveSystem;

    public InputManager(SaveSystem saveSystem)
    {
        _saveSystem = saveSystem;
    }

    public int GetFrameCount()
    {
        return _inputs.Count;
    }

    public OsContPad GetFrameInput(int frame)
    {
        OsContPad result = new OsContPad();
        if (frame < _inputs.Count && frame >= 0)
        {
            result.X = _inputs[frame].X;
            result.Y = _inputs[frame].Y;
            result.Buttons = _inputs[frame].Buttons;
        }

        return result;
    }

    public void SetFrameInput(int frame, OsContPad input, bool invalidate = true)
    {
        if (frame < _inputs.Count && frame >= 0)
        {
            if (_inputs[frame].X != input.X || _inputs[frame].Y != input.Y || _inputs[frame].Buttons != input.Buttons)
            {
                _inputs[frame].X = input.X;
                _inputs[frame].Y = input.Y;
                _inputs[frame].Buttons = input.Buttons;
                if (invalidate)
                {
                    _saveSystem.InvalidateFrames(frame);
                }
            }
        }
    }

    public void Tick()
    {
        AddInputFrames();
    }

    public void Clear()
    {
        _inputs.Clear();
    }

    public void LoadFrom(IList<InputModel> inputs)
    {
        Clear();
        if (inputs != null)
            foreach (InputModel input in inputs)
            {
                _inputs.Add(new InputModel(new byte[4])
                {
                    Buttons = input.Buttons,
                    X = input.X,
                    Y = input.Y
                });
            }
    }

    private void AddInputFrames()
    {
        int currentFrame = _saveSystem.CurrentFrame?.Frame ?? 0;
        while (currentFrame >= _inputs.Count - 1000)
        {
            for (int i = 0; i < 1000; i++)
            {
                _inputs.Add(new InputModel(new byte[4]));
            }
        }
    }
}