using MarioSharp;

namespace MarioIDE.Core.Logic;

public interface ISaveSystem
{
    IInputManager InputManager { get; }
    GameInstance GameInstance { get; }
    float Progress { get; }
    void Init(GfxType gfxType, byte[] dllBytes);
    void Tick();
    SaveSlot CurrentFrame { get; }
    void SetCurrentFrame(int frame);
    void Dispose();
}