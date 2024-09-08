using System.Runtime.InteropServices;
using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Surface
{
    public short type;
    public short force;
    public byte flags;
    public byte room;
    public short lowerY;
    public short upperY;
    public Vec3S vertex1;
    public Vec3S vertex2;
    public Vec3S vertex3;
    public Vector3 normal;
    public float originOffset;
    public IntPtr objectPtr;
};