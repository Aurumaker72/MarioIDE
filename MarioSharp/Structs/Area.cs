using System.Runtime.InteropServices;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Explicit)]
public struct Area
{
    [FieldOffset(0x48)] public IntPtr CameraPtr;
}