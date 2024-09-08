namespace MarioSharp.Internal;

internal class SectionInfo
{
    public string Name { get; }
    public IntPtr Address { get; }
    public int Size { get; }

    public SectionInfo(string name, IntPtr address, int size)
    {
        Name = name;
        Address = address;
        Size = size;
    }
}