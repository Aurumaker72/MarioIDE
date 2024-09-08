using Caliburn.Micro;
using Gemini;
using Gemini.Framework;
using Gemini.Framework.Themes;
using Gemini.Modules.StatusBar;
using Gemini.Modules.UndoRedo;
using MarioIDE.Core.Framework;
using MarioIDE.Core.Logic;
using MarioIDE.Core.Modules.Timeline;
using MarioIDE.Framework;
using MarioIDE.Framework.Models;
using MarioIDE.Framework.ViewModels;
using MarioIDE.Logic;
using MarioIDE.Modules.GameView.ViewModels;
using MarioIDE.Modules.Input.ViewModels;
using MarioIDE.Modules.SceneView.ViewModels;
using MarioIDE.Modules.Timeline.ViewModels;
using MarioSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MarioIDE;

[Export(typeof(IModule))]
[Export(typeof(IGlHandler))]
public class Module : ModuleBase, IGlHandler
{
    public static Window Window { get; private set; }
    public static IntPtr WindowHandle { get; private set; }
    public override IEnumerable<Type> DefaultTools => new[] { typeof(IInput), typeof(IHistoryTool), typeof(IGameView), typeof(ISceneView) };

    public static int MaxFPS = 480;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FrameCounter _frameCounter = new();
    private readonly IThemeManager _themeManager;
    private readonly IStatusBar _statusBar;

    private IPlaybackController _playbackController;
    private bool _closed;

    [ImportingConstructor]
    public Module(IThemeManager themeManager, IStatusBar statusBar)
    {
        _themeManager = themeManager;
        _statusBar = statusBar;
    }

    public override void Initialize()
    {
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Unlocker.LockRoms();
        Unlocker.UnlockRoms();

        Window = Application.Current.MainWindow;
        if (Window != null)
        {
            IntPtr baseHandle = new WindowInteropHelper(Window).Handle;
            WindowHandle = new HwndSource(0, 0, 0, 0, 0, string.Empty, baseHandle).Handle;
            Window.Closing += WindowOnClosing;
            Window.Closed += WindowOnClosed;
        }

        _themeManager.SetDefaultTheme("DarkTheme");

        Shell.ShowFloatingWindowsInTaskbar = true;
        Shell.ToolBars.Visible = true;
        Shell.ActiveDocumentChanging += ShellOnActiveDocumentChanging;

        MainWindow.Title = "MarioIDE";
        MainWindow.WindowState = WindowState.Maximized;

        _playbackController = IoC.Get<IPlaybackController>();

        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        Task.Factory.StartNew(() => { MainTask(dispatcher); });
    }

    private void MainTask(Dispatcher dispatcher)
    {
        while (!_closed)
        {
            dispatcher.Invoke(() =>
            {
                if (!_closed)
                {
                    while (_stopwatch.Elapsed.TotalMilliseconds < 1000.0 / MaxFPS)
                    {
                        //Thread.SpinWait(0);
                    }
                    _stopwatch.Restart();
                    Tick();
                }
            });
        }
    }

    private void Tick()
    {
        Time.Tick();
        _frameCounter.Update(Time.DeltaTime);

        if (Shell.ActiveItem is TimelineViewModel timeline)
        {
            _playbackController.Tick(timeline);
        }

        foreach (IGlView view in IoC.GetAll<IGlView>())
        {
            view.Render();
        }

        float progress = 0;
        int projectCount = 0;

        foreach (IProject project in IoC.GetAll<IProject>())
        {
            project.Tick();
            if (project.SaveSystem is SaveSystem saveSystem)
            {
                progress += saveSystem.Progress;
                projectCount++;
            }
        }

        progress /= projectCount;
        _statusBar.Progress = (int)Math.Min(100, progress * 100);
        _statusBar.Text = "FPS: " + (int)_frameCounter.AverageFramesPerSecond + ", Loads: " + GameInstance.LoadCount + ", Saves: " + GameInstance.SaveCount;

        GameInstance.ResetCounters();
    }

    private void ShellOnActiveDocumentChanging(object sender, EventArgs e)
    {
        _playbackController.SetPlaybackState(PlaybackState.Paused);
    }

    public void UnregisterGlView(ComposablePart part)
    {
        CompositionBatch batch = new();
        batch.RemovePart(part);
        AppBootstrapper.Container.Compose(batch);
    }

    public ComposablePart RegisterGlView(IGlView glView)
    {
        CompositionBatch batch = new();
        ComposablePart part = batch.AddExportedValue(glView);
        AppBootstrapper.Container.Compose(batch);
        return part;
    }

    private void WindowOnClosing(object sender, CancelEventArgs e)
    {
        _closed = true;
    }

    private static void WindowOnClosed(object sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}