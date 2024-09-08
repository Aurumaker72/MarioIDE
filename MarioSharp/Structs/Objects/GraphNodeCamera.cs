using System.Runtime.InteropServices;
using OpenTK;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Explicit)]
public struct GraphNodeCamera
{
    [FieldOffset(0x1C)]
    public Vector3 pos;
    [FieldOffset(0x34)]
    public IntPtr matrixPtr;
}