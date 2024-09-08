using System.Runtime.InteropServices;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Explicit)]
public struct Camera
{
    [FieldOffset(0x2)] public short Yaw;
}