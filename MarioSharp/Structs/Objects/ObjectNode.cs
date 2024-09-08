using System.Runtime.InteropServices;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct ObjectNode
{
    public GraphNodeObject gfx;
    public IntPtr next;
    public IntPtr prev;
}