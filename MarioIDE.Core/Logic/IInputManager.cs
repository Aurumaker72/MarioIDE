using MarioSharp.Structs.Input;

namespace MarioIDE.Core.Logic;

public interface IInputManager
{
    OsContPad GetFrameInput(int frame);
    void SetFrameInput(int frame, OsContPad input, bool invalidate = true);
    int GetFrameCount();
    void Clear();
    void Tick();
}