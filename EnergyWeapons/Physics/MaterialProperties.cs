using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace Equinox.EnergyWeapons.Physics
{
    public class MaterialProperties
    {
        public readonly MyDefinitionId Material;

        public MaterialProperties(MyDefinitionId material, float density, 
            float thermalConductivity,
            float meltPoint, float vaporPoint, float s, float hFus, float hVap)
        {
            Material = material;
            ThermalConductivity = thermalConductivity;
            SpecificHeat = s;
            EnthalpyOfFusion = hFus;
            EnthalpyOfVaporization = hVap;
            MeltingPoint = meltPoint;
            BoilingPoint = vaporPoint;
            DensitySolid = density;
        }

        /// <summary>
        /// kW/(m*K)
        /// </summary>
        /// <remarks>
        /// Heat transfer between two sides of a 1m^2 plate with the given thickness.
        /// </remarks>
        public readonly float ThermalConductivity;

        /// <summary>
        /// kg/m3
        /// </summary>
        public readonly float DensitySolid;

        /// <summary>
        /// kJ/(kg*K)
        /// </summary>
        public readonly float SpecificHeat;

        /// <summary>
        /// kJ/kg to go from solid to liquid
        /// </summary>
        public readonly float EnthalpyOfFusion;

        /// <summary>
        /// kJ/kg to go from liquid to vapor
        /// </summary>
        public readonly float EnthalpyOfVaporization;

        /// <summary>
        /// In K
        /// </summary>
        public readonly float MeltingPoint, BoilingPoint;

        public MaterialProperties Clone(MyDefinitionId @new, float scale = 1, float densityScale = 1)
        {
            return new MaterialProperties(@new, DensitySolid * scale * densityScale,
                ThermalConductivity * scale * densityScale,
                MeltingPoint * scale, BoilingPoint * scale, SpecificHeat * scale,
                EnthalpyOfFusion * scale, EnthalpyOfVaporization * scale);
        }


        public static MaterialProperties LinearCombination(MyDefinitionId @new, MaterialProperties a, float av,
            MaterialProperties b, float bv)
        {
            return new MaterialProperties(@new,
                a.DensitySolid * av + b.DensitySolid * bv,
                a.ThermalConductivity * av + b.ThermalConductivity * bv,
                a.MeltingPoint * av + b.MeltingPoint * bv,
                a.BoilingPoint * av + b.BoilingPoint * bv,
                a.SpecificHeat * av + b.SpecificHeat * bv,
                a.EnthalpyOfFusion * av + b.EnthalpyOfFusion * bv,
                a.EnthalpyOfVaporization * av + b.EnthalpyOfVaporization * bv);
        }
    }
}