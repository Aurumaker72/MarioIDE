using MarioIDE.Core.Logic;

namespace MarioIDE.Core.Modules.Timeline;

public interface IProject
{
    ISaveSystem SaveSystem { get; }
    int SelectionStart { get; }
    int SelectionEnd { get; }
    void SetSelectedRange(int selectionStart, int selectionEnd);
    void ScrollToFrame(int frame);
    void Tick();
}