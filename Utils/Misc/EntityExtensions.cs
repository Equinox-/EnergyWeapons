using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class EntityExtensions
    {
        public static string ToStringSmart(this IMyEntity e)
        {
            var term = e as IMyTerminalBlock;
            if (term != null)
                return $"{term.CubeGrid}/{term.Position}/{term.CustomName}";
            var block = e as IMyCubeBlock;
            if (block != null)
                return $"{block.CubeGrid}/{block.Position}";
            return e.ToString();
        }

        public static bool IsPhysicallyPresent(this IMyEntity e)
        {
            while (e != null)
            {
                if (e.Physics != null)
                    return true;
                e = e.Parent;
            }
            return false;
        }
    }
}
