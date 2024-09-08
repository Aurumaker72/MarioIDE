using Caliburn.Micro;
using MarioIDE.Framework.Models;
using MarioIDE.Framework.Views;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KeyboardState = MarioIDE.Framework.Models.KeyboardState;
using MouseState = MarioIDE.Framework.Models.MouseState;

namespace MarioIDE.Framework.ViewModels;

internal class GlViewModel : IDisposable
{
    public delegate void OnRenderDelegate();

    public event OnRenderDelegate OnRender;

    public delegate void GotFocusDelegate();

    public event GotFocusDelegate GotFocus;

    public MouseState MouseState { get; } = new();
    public KeyboardState KeyboardState { get; } = new();

    public int Width { get; private set; }
    public int Height { get; private set; }

    private readonly Color _backColor = Color.FromArgb(30, 30, 30);
    private readonly ImGuiController _controller;

    private SafeGlWindow _window;
    private ComposablePart _part;

    public GlViewModel()
    {
        CreateWindow();
        BindContext();
        GL.DrawBuffer(DrawBufferMode.Front);
        _controller = new ImGuiController(Width, Height);
    }

    public void OnViewLoaded(IGlView glView, GlControl control)
    {
        Register(glView);
        AttachControl(control);
    }

    public void OnViewUnloaded()
    {
        Unregister();
    }

    public void Render()
    {
        Begin();

        if (OnRender != null)
        {
            ImGuiUtils.DrawFullscreen(OnRender.Invoke);
        }

        End();
    }

    public void Begin()
    {
        BindContext();
        KeyboardState.Update();
        GL.Viewport(0, 0, Width, Height);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.ClearColor(_backColor);
        _controller.Update(Width, Height, MouseState, KeyboardState);
    }

    public void End()
    {
        _controller.Render();
        _window?.ProcessEvents();
        UnbindContext();
    }

    public void Dispose()
    {
        Unregister();
    }

    private void AttachControl(Control control)
    {
        SetWindowAsChild(control.Handle);
        Resize(control.Width, control.Height);
        control.Resize += (_, _) => Resize(control.Width, control.Height);
        control.GotFocus += (_, _) => GotFocus?.Invoke();
        control.LostFocus += ControlOnLostFocus;
        control.MouseDown += (_, args) => ControlOnMouseDown(control, args);
        control.MouseUp += ControlOnMouseUp;
        control.MouseMove += ControlOnMouseMove;
        control.MouseWheel += ControlOnMouseWheel;
        control.KeyPress += ControlOnKeyPress;
        control.PreviewKeyDown += ControlOnPreviewKeyDown;
        control.KeyDown += ControlOnKeyDown;
        control.KeyUp += ControlOnKeyUp;
    }

    private void ControlOnKeyDown(object sender, KeyEventArgs e)
    {
        KeyboardState.SetKeyState(e.KeyCode, true);
    }

    private void ControlOnKeyUp(object sender, KeyEventArgs e)
    {
        KeyboardState.SetKeyState(e.KeyCode, false);
    }

    private static void ControlOnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        e.IsInputKey = true;
    }

    private void ControlOnKeyPress(object sender, KeyPressEventArgs e)
    {
        _controller.PressChar(e.KeyChar);
    }

    private void ControlOnLostFocus(object sender, EventArgs e)
    {
        KeyboardState.Reset();
    }

    private void ControlOnMouseWheel(object sender, MouseEventArgs e)
    {
        MouseState.Wheel += e.Delta / 100;
    }

    private void ControlOnMouseDown(Control control, MouseEventArgs e)
    {
        control.Focus();
        Module.Window.Activate();
        control.Focus();

        if (e.Button == MouseButtons.Left)
        {
            MouseState.LeftButton = true;
        }
        else if (e.Button == MouseButtons.Right)
        {
            MouseState.RightButton = true;
        }
        else if (e.Button == MouseButtons.Middle)
        {
            MouseState.MiddleButton = true;
        }
    }

    private void ControlOnMouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            MouseState.LeftButton = false;
        }
        else if (e.Button == MouseButtons.Right)
        {
            MouseState.RightButton = false;
        }
        else if (e.Button == MouseButtons.Middle)
        {
            MouseState.MiddleButton = false;
        }
    }

    private void ControlOnMouseMove(object sender, MouseEventArgs e)
    {
        MouseState.X = e.X;
        MouseState.Y = e.Y;
    }

    private void Resize(int width, int height)
    {
        Width = width;
        Height = height;

        /*if (!_window.Visible) _window.Visible = true;
        _window.Location = new Point(0, 0);
        _window.Size = new Size(Width, Height);*/

        Win32.ShowWindow(_sdlHandle, 5);
        Win32.SetWindowPos(_sdlHandle, IntPtr.Zero, 0, 0, width, height, 0);
    }

    private IntPtr _sdlHandle;
    private string _uuid;
    private static readonly Random Random = new Random();

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    private void CreateWindow()
    {
        _uuid = RandomString(8);
        
        //Console.WriteLine("CreateWindow starting...");
        //Console.WriteLine("uuid: " + _uuid);
        
        _window = new SafeGlWindow(_uuid);

        // _window.WindowInfo.Handle gives the wrong handle on some systems. ugly workaround is to use FindWindow with a uuid
        _sdlHandle = Win32.FindWindow("SDL_app", "sdl-mario-ide-" + _uuid);
        
        //Console.WriteLine("sdlHandle: " + _sdlHandle);
    }

    private void SetWindowAsChild(IntPtr windowHandle)
    {
        Win32.SetParent(_sdlHandle, windowHandle);

        IntPtr style = (IntPtr)(long)(Win32.WindowStyles.WS_CHILD | Win32.WindowStyles.WS_DISABLED);
        Win32.SetWindowLongPtr(_sdlHandle, Win32.WindowLongs.GWL_STYLE, style);

        style = (IntPtr)(long)Win32.WindowStylesEx.WS_EX_NOACTIVATE;
        Win32.SetWindowLongPtr(_sdlHandle, Win32.WindowLongs.GWL_EXSTYLE, style);
    }

    private void BindContext()
    {
        if (_window != null && !_window.Context.IsCurrent)
        {
            _window?.Context?.MakeCurrent(_window.WindowInfo);
        }
    }

    private void UnbindContext()
    {
        if (_window != null && _window.Context.IsCurrent)
        {
            _window?.Context?.MakeCurrent(null);
        }
    }

    private void Register(IGlView glView)
    {
        _part = IoC.Get<IGlHandler>().RegisterGlView(glView);
    }

    private void Unregister()
    {
        if (_part != null)
        {
            IoC.Get<IGlHandler>().UnregisterGlView(_part);
            _part = null;
        }
    }
}