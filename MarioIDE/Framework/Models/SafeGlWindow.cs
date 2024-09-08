using OpenTK;
using OpenTK.Graphics;

namespace MarioIDE.Framework.Models
{
    public sealed class SafeGlWindow : NativeWindow
    {
        public IGraphicsContext Context { get; }

        public SafeGlWindow(string uuid) : base(640, 480, "sdl-mario-ide-" + uuid, GameWindowFlags.Default, GraphicsMode.Default, DisplayDevice.Default)
        {
            Context = new GraphicsContext(GraphicsMode.Default, WindowInfo, 1, 0, GraphicsContextFlags.Default);
            Context.MakeCurrent(WindowInfo);
            ((IGraphicsContextInternal)Context).LoadAll();
            WindowBorder = WindowBorder.Hidden;
            WindowState = WindowState.Normal;
            Visible = false;
        }
    }
}
