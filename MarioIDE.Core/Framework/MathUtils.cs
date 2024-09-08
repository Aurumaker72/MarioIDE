using OpenTK;

namespace MarioIDE.Core.Framework;

public class MathUtils
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp01(t);
    }

    public static int Clamp(int value, int min, int max)
    {
        return MathHelper.Clamp(value, min, max);
    }

    public static int Clamp01(int value)
    {
        return Clamp(value, 0, 1);
    }

    public static float Clamp(float value, float min, float max)
    {
        return MathHelper.Clamp(value, min, max);
    }

    public static float Clamp01(float value)
    {
        return Clamp(value, 0, 1);
    }

    public static Vector3 AngleToDirection(float pitch, float yaw)
    {
        return new Vector3(
            (float)(Math.Cos(pitch) * Math.Sin(yaw)),
            (float)Math.Sin(pitch),
            (float)(Math.Cos(pitch) * Math.Cos(yaw)));
    }
}