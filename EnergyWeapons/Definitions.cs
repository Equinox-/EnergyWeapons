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
            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser"),
                Components = new List<Component>()
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .5e3f,
                        CoolingPower = .1f,
                        Efficiency = 1f,
                        ThermalFuseMin = 1000,
                        ThermalFuseMax = 1500,
                        Dummy = "MissileTurretBase1/laser_a1",
                    },
                    new Optics()
                    {
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "beam_acceptor_01"},
                        OutgoingBeams = new[]
                        {
                            new Optics.OutgoingBeam()
                            {
                                Color = Vector4.One,
                                Dummy = "MissileTurretBase1/MissileTurretBarrels/laser_b2",
                                MaxThroughput = float.PositiveInfinity
                            }
                        },
                        IntersectionPoint = "MissileTurretBase1/laser_b1",
                    },
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "MissileTurretBase1/MissileTurretBarrels/laser_b2",
                            "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001"
                        }
                    },
                    new Weapon()
                    {
                        MaxLazeDistance = 1e4f,
                        CoolingPower = .1f,
                        Efficiency = 1f,
                        WeaponDamageMultiplier = 100f,
                        FxImpactName = "WelderContactPoint",
                        FxImpactBirthRate = 2,
                        FxImpactScale = 3f,
                        FxImpactMaxCount = 25,
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001"
                    }
                }
            });


            {
                const float power = 2.5e6f;
                set.Beams.Add(new Block()
                {
                    Id = new MyDefinitionId(typeof(MyObjectBuilder_UpgradeModule), "MA_BN_T1PSU_Large"),
                    Components = new List<Component>()
                    {
                        new Emitter()
                        {
                            ColorMin = new Vector4(1, 1, 1, 0.25f),
                            ColorMax = new Vector4(1, 1, 1, 1),
                            CoolingPower = 1f,
                            MaxPowerOutput =power/ 4,
                            AutomaticTurnOff = true,
                            Efficiency = 1f,
                            Dummy = "laser_a1"
                        },
                        new Path()
                        {
                            Dummies = new[] {"laser_a1", "laser_a2", "laser_c1", "laser_c2"}
                        },
                        new Emitter()
                        {
                            ColorMin = new Vector4(1, 1, 1, 0.25f),
                            ColorMax = new Vector4(1, 1, 1, 1),
                            CoolingPower = 1f,
                            MaxPowerOutput = power / 4,
                            AutomaticTurnOff = true,
                            Efficiency = 1f,
                            Dummy = "laser_b1"
                        },
                        new Path()
                        {
                            Dummies = new[] {"laser_b1", "laser_b2", "laser_d1", "laser_d2"}
                        },
                        new Emitter()
                        {
                            ColorMin = new Vector4(1, 1, 1, 0.25f),
                            ColorMax = new Vector4(1, 1, 1, 1),
                            CoolingPower = 1f,
                            MaxPowerOutput = power / 4,
                            AutomaticTurnOff = true,
                            Efficiency = 1f,
                            Dummy = "laser_b4"
                        },
                        new Path()
                        {
                            Dummies = new[] {"laser_b4", "laser_b3", "laser_f1", "laser_f2"}
                        },
                        new Emitter()
                        {
                            ColorMin = new Vector4(1, 1, 1, 0.25f),
                            ColorMax = new Vector4(1, 1, 1, 1),
                            CoolingPower = 1f,
                            MaxPowerOutput = power / 4,
                            AutomaticTurnOff = true,
                            Efficiency = 1f,
                            Dummy = "laser_a4"
                        },
                        new Path()
                        {
                            Dummies = new[] {"laser_a4", "laser_a3", "laser_e1", "laser_e2"}
                        },
                        new Optics()
                        {
                            IncomingBeams = new[] {"laser_c2", "laser_d2", "laser_f2", "laser_e2"},
                            OutgoingBeams = new[]
                            {
                                new Optics.OutgoingBeam()
                                {
                                    Dummy = "beam_emitter_01",
                                    Color = Vector4.One,
                                    MaxThroughput = float.PositiveInfinity
                                }
                            },
                            IntersectionPoint = "laser_g1"
                        }
                    }
                });
            }


            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_Passage), "MA_BN_Tube0_Large"),
                Components = new List<Component>()
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01", "beam_acceptor_02"
                        }
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_Passage), "MA_BN_Tubex3_Large"),
                Components = new List<Component>()
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_03", "beam_acceptor_04"
                        }
                    }
                }
            });

            #region Test Optics
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
                        }
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
                        }
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
                        Dummies = new[] {"conn_211", "conn_111", "conn_011"}
                    }
                }
            });
            #endregion
        }
    }
}