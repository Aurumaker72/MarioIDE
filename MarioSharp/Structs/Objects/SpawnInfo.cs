using System.Runtime.InteropServices;
using MarioSharp.Structs.Math;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct SpawnInfo
{
    public Vec3S startPos;
    public Vec3S startAngle;
    public byte areaIndex;
    public byte activeAreaIndex;
    public uint behaviorArg;
    public IntPtr behaviorScript;
    public IntPtr unk18;
    public IntPtr next;
}