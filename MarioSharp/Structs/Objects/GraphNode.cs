using System.Runtime.InteropServices;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct GraphNode
{
    public short type; // structure type
    public short flags; // hi = drawing layer, lo = rendering modes
    public IntPtr prev;
    public IntPtr next;
    public IntPtr parent;
    public IntPtr children;
}