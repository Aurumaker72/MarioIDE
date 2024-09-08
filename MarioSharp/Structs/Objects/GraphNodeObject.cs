using System.Runtime.InteropServices;
using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioSharp.Structs.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct GraphNodeObject
{
    public GraphNode node;
    public IntPtr sharedChild;
    public byte areaIndex;
    public byte activeAreaIndex;
    public Vec3S angle;
    public Vector3 pos;
    public Vector3 scale;
    public AnimInfo animInfo;
    public IntPtr spawnInfo;
    public IntPtr throwMatrix; // matrix ptr
    public Vector3 cameraToObject;
}