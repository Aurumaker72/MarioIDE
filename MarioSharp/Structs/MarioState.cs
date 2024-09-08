using System.Runtime.InteropServices;
using MarioSharp.Structs.Math;
using OpenTK;

namespace MarioSharp.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct MarioState
{
    public ushort unk00;
    public ushort input;
    public uint flags;
    public uint particleFlags;
    public uint action;
    public uint prevAction;
    public uint terrainSoundAddend;
    public ushort actionState;
    public ushort actionTimer;
    public uint actionArg;
    public float intendedMag;
    public ushort intendedYaw;
    public short invincTimer;
    public byte framesSinceA;
    public byte framesSinceB;
    public byte wallKickTimer;
    public byte doubleJumpTimer;
    public Vec3S faceAngle;
    public Vec3S angleVel;
    public short slideYaw;
    public short twirlYaw;
    public Vector3 pos;
    public Vector3 vel;
    public float forwardVel;
    public float slideVelX;
    public float slideVelZ;
    public IntPtr wallPointer;
    public IntPtr ceilPointer;
    public IntPtr floorPointer;
    public float ceilHeight;
    public float floorHeight;
    public short floorAngle;
    public short waterLevel;
    public IntPtr interactObjPointer;
    public IntPtr heldObjPointer;
    public IntPtr usedObjPointer;
    public IntPtr riddenObjPointer;
    public IntPtr marioObjPointer;
    public IntPtr spawnInfoPointer;
    public IntPtr areaPointer;
    public IntPtr statusForCameraPointer;
    public IntPtr marioBodyStatePointer;
    public IntPtr controllerPointer;
    public IntPtr animationPointer;
    public uint collidedObjInteractTypes;
    public short numCoins;
    public short numStars;
    public byte numKeys;
    public byte numLives;
    public short health;
    public short unkB0;
    public byte hurtCounter;
    public byte healCounter;
    public byte squishTimer;
    public byte fadeWarpOpacity;
    public ushort capTimer;
    public short prevNumStarsForDialog;
    public float peakHeight;
    public float quicksandDepth;
    public float unkC4;

    public float GetHorizontalSlidingSpeed()
    {
        double x = System.Math.Pow(slideVelX, 2);
        double z = System.Math.Pow(slideVelZ, 2);
        return (float)System.Math.Sqrt(x + z);
    }
}