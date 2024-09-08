using System.Runtime.InteropServices;

namespace MarioSharp.Structs.Math;

[StructLayout(LayoutKind.Sequential)]
public struct Vec3S
{
    public static readonly Vec3S Zero;
    public static readonly Vec3S One = new(1, 1, 1);

    public short X;
    public short Y;
    public short Z;

    public Vec3S(short x, short y, short z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString()
    {
        return $"X={X}, Y={Y}, Z={Z}";
    }
}