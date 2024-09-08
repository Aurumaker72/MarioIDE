using System.Numerics;
using OpenTK;

namespace MarioIDE.Core.Framework
{
    public static class Extensions
    {
        public static Matrix4 ToMatrix4(this Matrix4x4 from)
        {
            return new Matrix4(
                from.M11, from.M12, from.M13, from.M14,
                from.M21, from.M22, from.M23, from.M24,
                from.M31, from.M32, from.M33, from.M34,
                from.M41, from.M42, from.M43, from.M44
            );
        }

        public static Matrix4x4 ToMatrix4X4(this Matrix4 from)
        {
            return new Matrix4x4(
                from.M11, from.M12, from.M13, from.M14,
                from.M21, from.M22, from.M23, from.M24,
                from.M31, from.M32, from.M33, from.M34,
                from.M41, from.M42, from.M43, from.M44
            );
        }
    }
}
