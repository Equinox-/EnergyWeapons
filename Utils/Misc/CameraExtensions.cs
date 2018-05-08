using VRage.ModAPI;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class CameraExtensions
    {
        public static bool IsInFrustum(this IMyCamera cam, ref Vector3D point)
        {
            return cam.WorldToScreen(ref point).AbsMax() <= 1;
        }
    }
}
