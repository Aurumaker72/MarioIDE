using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioIDE.Logic;

internal class FrameData
{
    public short Level;
    public byte Area;
    public uint Action;
    public float HSpeed;
    public float YSpeed;
    public float HSlidingSpeed;
    public Vector3 MarioPos;
    public Vec3S FaceAngle;
    public int CameraYaw;
    public ushort IntendedYaw;
    public ushort RngValue;
    public float SpdEfficiency;
    public float X;
    public float Y;
    public float Z;
}