using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Physics
{
    public static class PhysicalConstants
    {
        /// <summary>
        /// Temperature in space (K)
        /// </summary>
        public static readonly float TemperatureSpace = 5;

        /// <summary>
        /// Temperature on surface of a planet (K)
        /// </summary>
        public static readonly float TemperatureSurface = 300;

        /// <summary>
        /// Computes the temperature at a given point in space.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <returns>Background temperature (K)</returns>
        public static float TemperatureAtPoint(Vector3D pos)
        {
            var planet = MyGamePruningStructure.GetClosestPlanet(pos);
            if (planet != null)
            {
                var density = planet.GetAirDensity(pos);
                return MathHelper.Lerp(TemperatureSpace, TemperatureSurface, MathHelper.Clamp(density * density, 0, 1));
            }

            return TemperatureSpace;
        }
    }
}