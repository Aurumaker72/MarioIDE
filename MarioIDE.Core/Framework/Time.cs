using System.Diagnostics;

namespace MarioIDE.Core.Framework;

public static class Time
{
    public static float TotalTime => (float)Stopwatch.Elapsed.TotalSeconds;
    public static float DeltaTime { get; private set; }

    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    private static readonly Stopwatch DeltaStopwatch = Stopwatch.StartNew();

    public static void Tick()
    {
        DeltaTime = (float)DeltaStopwatch.Elapsed.TotalSeconds;
        DeltaStopwatch.Restart();
    }
}