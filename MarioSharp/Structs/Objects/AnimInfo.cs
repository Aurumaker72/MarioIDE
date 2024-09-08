using System.Runtime.InteropServices;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct AnimInfo
{
    public short animID;
    public short animYTrans;
    public IntPtr curAnim;
    public short animFrame;
    public ushort animTimer;
    public int animFrameAccelAssist;
    public int animAccel;
}