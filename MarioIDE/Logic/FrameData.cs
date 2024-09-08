using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioIDE.Logic;

internal class FrameData
{
    public short Level;
    public byte Area;
    public uint Action;
    public float HSpeed;
    public float HSlidingSpeed;
    public Vector3 MarioPos;
    public Vec3S FaceAngle;
    public int CameraYaw;
}