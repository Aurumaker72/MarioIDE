using System.Runtime.InteropServices;
using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct LakituState
{
    public Vector3 CurFocus;
    public Vector3 CurPos;
    public Vector3 GoalFocus;
    public Vector3 GoalPos;
    public Vector3 Filler30;
    public byte Mode;
    public byte DefMode;
    public long Filler3E;
    public short Filler3D;
    public float FocusDistance;
    public short OldPitch;
    public short OldYaw;
    public short OldRoll;
    public Vec3S ShakeMagnitude;
    public short ShakePitchPhase;
    public short ShakePitchVel;
    public short ShakePitchDecay;
    public Vector3 UnusedVec1;
    public Vec3S UnusedVec2;
    public long Filler72;
    public short Roll;
    public short Yaw;
    public short NextYaw;
    public Vector3 Focus;
    public Vector3 Pos;
    public short ShakeRollPhase;
    public short ShakeRollVel;
    public short ShakeRollDecay;
    public short ShakeYawPhase;
    public short ShakeYawVel;
    public short ShakeYawDecay;
    public float FocHSpeed;
    public float FocVSpeed;
    public float PosHSpeed;
    public float PosVSpeed;
    public short KeyDanceRoll;
    public int LastFrameAction;
    public short Unused;
}