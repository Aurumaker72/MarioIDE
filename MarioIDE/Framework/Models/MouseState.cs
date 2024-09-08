namespace MarioIDE.Framework.Models;

public class MouseState
{
    public int X { get; internal set; }
    public int Y { get; internal set; }
    public int Wheel { get; internal set; }
    public bool LeftButton { get; internal set; }
    public bool RightButton { get; internal set; }
    public bool MiddleButton { get; internal set; }
}