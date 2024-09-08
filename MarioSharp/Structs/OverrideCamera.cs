using System.Runtime.InteropServices;
using OpenTK;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct OverrideCamera
{
    public bool enabled;
    public Vector3 pos;
    public Vector3 focus;
    public float roll;
}