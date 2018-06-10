using System.Collections.Generic;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Beam;
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
            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored"),
                Components = new List<Component>
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
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_a1.2"},
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
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "MissileTurretBase1/laser_a1.2"
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored_Slope"),
                Components = new List<Component>
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
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_a1.2"},
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
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "MissileTurretBase1/laser_a1.2"
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored_Slope2"),
                Components = new List<Component>
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
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_a1.2"},
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
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "MissileTurretBase1/laser_a1.2"
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored_Slope45"),
                Components = new List<Component>
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
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_a1.2"},
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
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "MissileTurretBase1/laser_a1.2"
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser"),
                Components = new List<Component>
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
                        IncomingBeams = new[] {"MissileTurretBase1/laser_a1", "MissileTurretBase1/laser_a1.2"},
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
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "MissileTurretBase1/laser_a1.2"
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored_sb"),
                Components = new List<Component>
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .1e3f,
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_Armored_Slope_sb"),
                Components = new List<Component>
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .1e3f,
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret),
                    "MA_Gimbal_Laser_Armored_Slope2_sb"),
                Components = new List<Component>
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .1e3f,
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret),
                    "MA_Gimbal_Laser_Armored_Slope45_sb"),
                Components = new List<Component>
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .1e3f,
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_Gimbal_Laser_sb"),
                Components = new List<Component>
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(.1f, 0, .1f, .1f),
                        ColorMax = new Vector4(.3f, 0, .8f, 1f),
                        MaxPowerOutput = .1e3f,
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });


            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_Passage), "MA_BN_Tube90_Large"),
                Components = new List<Component>()
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_08", "beam_dumdum_90", "beam_acceptor_09"
                        }
                    }
                }
            });

            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_Passage), "MA_BN_Combiner_Large"),
                Components = new List<Component>()
                {
                    new Optics()
                    {
                        IncomingBeams = new[] {"laser_h3", "laser_i3", "beam_acceptor_06"},
                        IntersectionPoint = "beam_emitter_02",
                        OutgoingBeams = new[]
                        {
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "beam_emitter_02",
                                Color = new Vector4(1, 1, 1, 1)
                            }
                        }
                    },
                    new Path()
                    {
                        Dummies = new[] {"beam_acceptor_05", "laser_h1", "laser_h2", "laser_h3"}
                    },
                    new Path()
                    {
                        Dummies = new[] {"beam_acceptor_07", "laser_i1", "laser_i2", "laser_i3"}
                    }
                }
            });


            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_Passage), "MA_BN_Splitter_Large"),
                Components = new List<Component>()
                {
                    new Optics()
                    {
                        IncomingBeams = new[] {"beam_acceptor_10"},
                        IntersectionPoint = "beam_splitter_01",
                        OutgoingBeams = new[]
                        {
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "beam_emitter_03",
                                Color = new Vector4(1, 1, 1, 1)
                            },
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "beam_emitter_04",
                                Color = new Vector4(1, 1, 1, 1)
                            },
                            new Optics.OutgoingBeam()
                            {
                                Dummy = "beam_emitter_05",
                                Color = new Vector4(1, 1, 1, 1)
                            }
                        }
                    },
                }
            });


            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_UpgradeModule), "MA_BN_T1PSU_Large"),
                Components = new List<Component>()
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(1, 0, 0, 0.25f),
                        ColorMax = new Vector4(1, 0, 0, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 5e3f / 4,
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
                        ColorMin = new Vector4(1, 0, 0, 0.25f),
                        ColorMax = new Vector4(1, 0, 0, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 5e3f / 4,
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
                        ColorMin = new Vector4(1, 0, 0, 0.25f),
                        ColorMax = new Vector4(1, 0, 0, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 5e3f / 4,
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
                        ColorMin = new Vector4(1, 0, 0, 0.25f),
                        ColorMax = new Vector4(1, 0, 0, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 5e3f / 4,
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


            set.Beams.Add(new Block()
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_UpgradeModule), "MA_BN_T2PSU_Large"),
                Components = new List<Component>()
                {
                    new Emitter()
                    {
                        ColorMin = new Vector4(1, 0, 0, 0.25f),
                        ColorMax = new Vector4(1, .3f, 0, 1),
                        CoolingPower = 1f,
                        MaxPowerOutput = 15e3f,
                        AutomaticTurnOff = true,
                        Efficiency = 1f,
                        Dummy = "beam_emitter_T2"
                    },
                }
            });


            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_000"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_001"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_002"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_003"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_004"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_005"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_000"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_001"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_002"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_003"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_004"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });

            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "MA_Fixed_sb_005"),
                Components = new List<Component>
                {
                    new Path()
                    {
                        Dummies = new[]
                        {
                            "beam_acceptor_01",
                            "muzzle_missile_001"
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
                        Dummy = "muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });


            set.Beams.Add(new Block
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "MA_PDX"),
                Components = new List<Component>
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
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
                        Dummy = "MissileTurretBase1/MissileTurretBarrels/muzzle_missile_001",
                        VoxelDamageMultiplier = 0
                    }
                }
            });
        }
    }
}