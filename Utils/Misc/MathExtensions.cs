using System;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class MathExtensions
    {
        public const float EPSILON = 1e-6f;

        public static bool EqualsEps(this Vector4 a, Vector4 b, float eps = EPSILON)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z) < eps;
        }

        public static bool EqualsEps(this float a, float b, float eps = EPSILON)
        {
            return Math.Abs(a - b) < eps;
        }
    }
}
