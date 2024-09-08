using MarioIDE.Core.Modules.Timeline;

namespace MarioIDE.Core.Logic;

public interface IPlaybackController
{
    void SetPlaybackState(PlaybackState state);
    PlaybackState GetPlaybackState();
    void Tick(IProject timeline);
    void StepBackward(IProject timeline);
    void StepForward(IProject timeline);
    void SetCurrentFrame(IProject timeline, int frame);
}