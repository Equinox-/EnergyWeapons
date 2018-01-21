using System.Collections.Generic;
using Equinox.EnergyWeapons.Components.Weapon;
using Equinox.EnergyWeapons.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Weapon
{
    public class LaserWeaponDefinition : EnergyWeaponDefinition
    {
        public LaserWeaponDefinition(MyDefinitionId id) : base(id)
        {
        }

        /// <summary>
        /// Emissive material with emissivity controlled by the laser's heat.  Full emissivity = at meltdown temperature.
        /// </summary>
        public string LaserHeatEmissives { get; set; } = "LaserHeat";

        /// <summary>
        /// Emissive material with emissivity controlled by the beam's power.  Full power = full emissivity.
        /// </summary>
        public string LaserBeamEmissives { get; set; } = "LaserBeam";

        /// <summary>
        /// List of dummy *paths* to connect.  Dummy paths = subpart names, then dummy name.  Separate by a /.
        /// </summary>
        /// <example>
        /// {"GatlingTurretBase1/GatlingTurretBase2/GatlingBarrel/LaseP1", 
        ///     "GatlingTurretBase1/GatlingTurretBase2/GatlingBarrel/LaseP2"}
        /// means connect the dummy "LaseP1" on the GatlingBarrel subpart to the dummy "LaseP2" on the GatlingBarrel subpart.
        /// </example>
        public string[][] InternalBeams { get; set; } = null;

        /// <summary>
        /// Raycast begins this far forward of the muzzle barrel
        /// </summary>
        public double RayEmitterOffset { get; set; } = 2.5d;

        /// <summary>
        /// The visual laser starts this far forward of the muzzle barrel.
        /// </summary>
        public double LaserBeamEmitterOffset { get; set; } = 0;

        /// <summary>
        /// Maximum distance the laser can travel
        /// </summary>
        public double MaxLazeDistance { get; set; } = 1e4f;

        /// <summary>
        /// Maximum thickness of the laser 
        /// </summary>
        public float MaxLazeThickness { get; set; } = 0.5f;

        /// <summary>
        /// Maximum power the laser can support, in MW.
        /// </summary>
        public float MaxPowerOutput { get; set; } = 1f; // 1 MW

        /// <summary>
        /// Rate the weapon cools at, in MW/K.
        /// </summary>
        /// <remarks>
        /// Can be upgraded with additively with <see cref="LaserWeaponComponent.UpgradeValueCooling"/>;
        /// formula at <see cref="LaserWeaponComponent.CoolingPower"/>.
        /// </remarks>
        public float CoolingPower { get; set; } = .1f;

        /// <summary>
        /// Amount of input power that goes into the actual laser.
        /// (1 - ThisValue) * CurrentPower is lost as heat.
        /// </summary>
        /// <remarks>
        /// Can be upgraded with multiplicatively with <see cref="LaserWeaponComponent.UpgradeValueEfficiency"/>;
        /// formula at <see cref="LaserWeaponComponent.Efficiency"/>
        /// </remarks>
        public float Efficiency { get; set; } = 1f;

        /// <summary>
        /// Multiplier applied after lase power calculation.
        /// This is "cheaty" power that comes from nowhere.
        /// </summary>
        public float LasePowerMultiplier { get; set; } = 100f;

        /// <summary>
        /// Temperature in K at which the laser begins to meltdown.
        /// Null to auto compute from material properties (<see cref="MaterialPropertyDatabase"/>)
        /// </summary>
        /// <example>
        /// Highest steady state power output for a given temperature is CoolingPower * (Temperature - BackgroundTemp),
        /// where BackgroundTemp is calculated with <see cref="Physics.PhysicalConstants.TemperatureAtPoint"/>.
        /// </example>
        public float? MeltdownTemperature { get; set; } = 2000;

        /// <summary>
        /// The laser must drop below this temperature after overheating before it will start firing again.
        /// If null it's assumed to be <see cref="ThermalFuseMax"/>
        /// </summary>
        public float? ThermalFuseMin { get; set; } = 1000;

        /// <summary>
        /// Temperature in K at which the laser automatically stops firing, or null if this isn't supported.
        /// </summary>
        public float? ThermalFuseMax { get; set; } = 1500;

        /// <summary>
        /// Laser color at min power.
        /// </summary>
        public Vector4 LaserColorMin { get; set; } = new Vector4(.1f, 0, 0, .1f);

        /// <summary>
        /// Laser color at max power
        /// </summary>
        public Vector4 LaserColorMax { get; set; } = new Vector4(1f, 0, 0, 1f);

        /// <summary>
        /// Particle effect spawned at impact point: Name
        /// </summary>
        public string FxImpactName { get; set; } = "WelderContactPoint";

        /// <summary>
        /// Particle effect spawned at impact point: Birth rate
        /// </summary>
        public int FxImpactBirthRate { get; set; } = 2;

        /// <summary>
        /// Particle effect spawned at impact point: Scale
        /// </summary>
        public float FxImpactScale { get; set; } = 3f;

        /// <summary>
        /// Particle effect spawned at impact point: Maximum count
        /// </summary>
        public int FxImpactMaxCount { get; set; } = 25;

        public override bool GenerateAmmo => true;
    }
}