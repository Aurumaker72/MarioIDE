using MarioIDE.Core.Logic;
using MarioIDE.Core.Modules.Timeline;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace MarioIDE.Logic;

[Export(typeof(IPlaybackController))]
internal class PlaybackController : IPlaybackController
{
    private PlaybackState _playbackState = PlaybackState.Paused;
    private readonly Stopwatch _stopwatch = new();

    public PlaybackController()
    {
        _stopwatch.Start();
    }

    public void SetPlaybackState(PlaybackState playbackState)
    {
        _playbackState = playbackState;
    }

    public PlaybackState GetPlaybackState()
    {
        return _playbackState;
    }

    public void StepForward(IProject timeline)
    {
        SaveSystem saveSystem = (SaveSystem)timeline.SaveSystem;
        if (saveSystem.GameInstance != null)
        {
            saveSystem.SetCurrentFrame(saveSystem.CurrentFrame.Frame + 1);
            OnFrameChanged(timeline);
        }
    }

    public void StepBackward(IProject timeline)
    {
        SaveSystem saveSystem = (SaveSystem)timeline.SaveSystem;
        if (saveSystem.GameInstance != null)
        {
            saveSystem.SetCurrentFrame(saveSystem.CurrentFrame.Frame - 1);
            OnFrameChanged(timeline);
        }
    }

    public void SetCurrentFrame(IProject timeline, int frame)
    {
        if (timeline.SaveSystem.CurrentFrame.Frame != frame)
        {
            timeline.SaveSystem.SetCurrentFrame(frame);
            OnFrameChanged(timeline);
        }
    }

    public void Tick(IProject timeline)
    {
        switch (_playbackState)
        {
            case PlaybackState.PlayingForward:
                StepForward(timeline);
                break;
            case PlaybackState.PlayingBackward:
                StepBackward(timeline);
                break;
            case PlaybackState.Paused:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void OnFrameChanged(IProject timeline)
    {
        int currentFrame = timeline.SaveSystem.CurrentFrame.Frame;
        timeline.SetSelectedRange(currentFrame, currentFrame);
        timeline.ScrollToFrame(currentFrame);
        //timeline.SaveSystem.SetCurrentFrame(currentFrame);
    }
}