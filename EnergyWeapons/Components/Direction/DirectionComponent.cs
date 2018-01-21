using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Direction
{
    public abstract class DirectionComponent : MyEntityComponentBase
    {
        public abstract Vector3D ShotOrigin { get; }
        public abstract Vector3D ShotDirection { get; }
    }
}