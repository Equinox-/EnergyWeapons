using System.Collections.Generic;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Definition.Weapon;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace Equinox.EnergyWeapons
{
    public static class Definitions
    {
        public static void Create(DefinitionSet set)
        {
            foreach (var subtype in new[]
            {
                "KECLaserA",
                "KECLaserB",
                "KECLaserC",
                "KECLaserD",
                "KECParticleA",
                "KECGaussA",
                "K_EC_Shockwave"
            })
                set.Definitions.Add(
                    new LaserWeaponDefinition(new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), subtype)));


            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored"),
                Components = new List<Component>
                {
                    new Weapon
                    {
                        MaxLazeDistance = 1e4f,
                        FxImpactName = "WelderContactPoint",
                        FxImpactBirthRate = 2,
                        FxImpactScale = 3f,
                        FxImpactMaxCount = 25,
                        Efficiency = 1f,
                        CoolingPower = .1f,
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        WeaponDamageMultiplier = 100
                    },
                    new Path
                    {
                        Bidirectional = false,
                        Dummies = new[]
                        {
                            "beam_acceptor_01", "MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_b1",
                            "MissileTurretBase1/MissileTurretBarrels/laser_b2",
                            "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001"
                        }
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "OpticalTestBed"),
                Components = new List<Component>()
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "conn_011", "conn_111", "conn_211"
                        },
                        Bidirectional = true
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "OpticalBender"),
                Components = new List<Component>()
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "conn_011", "conn_111", "conn_101"
                        },
                        Bidirectional = true
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "OpticalSplitterCombiner"),
                Components = new List<Component>()
                {
                    new Optics()
                    {
                        IncomingBeams = new[] {"conn_011", "conn_101"},
                        IntersectionPoint = "conn_111",
                        OutgoingBeams = new[]
                        {
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "conn_211",
                                Color = new Vector4(1, 0, 0, 1)
                            },
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "conn_121",
                                Color = new Vector4(0, 1, 0, 1)
                            }
                        }
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_UpgradeModule), "OpticalEmitter"),
                Components = new List<Component>()
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(1, 1, 1, 0.25f),
                        ColorMax = new Vector4(1, 1, 1, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 300e3f,
                        AutomaticTurnOff = true,
                        Efficiency = 1f,
                        Dummy = "conn_211"
                    },
                    new Path()
                    {
                        Dummies = new[] {"conn_211", "conn_111", "conn_011"},
                        Bidirectional = false
                    }
                }
            });
        }
    }
}