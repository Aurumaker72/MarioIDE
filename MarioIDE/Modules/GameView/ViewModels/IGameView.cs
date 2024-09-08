using Gemini.Framework;

namespace MarioIDE.Modules.GameView.ViewModels;

public interface IGameView : ITool
{
    int CameraYaw { get; }
}