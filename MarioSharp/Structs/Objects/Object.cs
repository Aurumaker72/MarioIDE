using System.Runtime.InteropServices;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential, Size = 0x570)]
public struct Object
{
    public ObjectNode header;
    public IntPtr parentObj;
    public IntPtr prevObj;
    public uint collidedObjInteractTypes;
    public short activeFlags;
}