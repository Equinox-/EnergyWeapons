using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components;
using Equinox.EnergyWeapons.Components.Beam;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.EnergyWeapons.Components.Weapon;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Weapon;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Render;
using Equinox.Utils.Scheduler;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Generics;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class EnergyWeaponsCore : MySessionComponentBase
    {
        private const long MOD_MESSAGE_PING_MASTER_CHANNEL = 442403541L;
        private const long MOD_MESSAGE_PING_SLAVE_CHANNEL = MOD_MESSAGE_PING_MASTER_CHANNEL + 1;
        private const long MOD_MESSAGE_DEFINITION_CHANNEL = MOD_MESSAGE_PING_MASTER_CHANNEL + 2;

        public CustomLogger Logger => LoggerStatic;

        private static CustomLogger _loggerStatic;

        public static CustomLogger LoggerStatic => _loggerStatic ?? (_loggerStatic = new CustomLogger());

        private bool _master = false;
        private bool _init = false;
        public EntityComponentRegistry ComponentRegistry { get; private set; }

        public DefinitionManager Definitions { get; private set; }
        public MaterialPropertyDatabase Materials { get; private set; }
        public ThermalPhysicsController Physics { get; private set; }
        public UpdateScheduler Scheduler { get; private set; }

        private int _ticksUntilStartup;

        public override void UpdateAfterSimulation()
        {
            if (!_init)
                DoInit();
            if (!_master)
                return;
            if (_ticksUntilStartup >= 0)
            {
                if (_ticksUntilStartup == 0)
                {
                    ComponentRegistry.Attach();
                    Logger.Info("Attaching component system; no further factory registration");
                }

                _ticksUntilStartup--;
            }

            Scheduler.RunUpdate(1);

            Logger.UpdateAfterSimulation();
            Physics.Update();

            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.O))
            {
                var caster = MyAPIGateway.Session.LocalHumanPlayer?.Character?.EquippedTool?.Components
                    .Get<MyCasterComponent>();
                IMySlimBlock block = caster?.HitBlock;
                if (block != null)
                {
                    var tm = block?.FatBlock as IMyTerminalBlock;
                    if (tm != null)
                        msg.Append("Block ").AppendLine(tm.CustomName);
                    else
                        msg.Append("Slim ").AppendLine(block.Position.ToString());
                    var phys = Physics.PhysicsFor(block, false);
                    if (phys != null)
                    {
                        msg.Append(phys.Temperature).AppendLine(" K");
                    }

                    var component = block?.FatBlock?.Components.Get<NetworkComponent>();
                    if (component != null)
                    {
                        msg.Append(" BeamNet: ").AppendLine(component.Dummies.Count.ToString());
                        foreach (var c in component.Dummies.Select(x => x.Segment).Distinct())
                            msg.AppendFormat("{0:X8} => {1:F2} kJ\n", c.GetHashCode(), c.CurrentEnergy);
                    }

                    MyAPIGateway.Utilities.ShowNotification(msg.ToString());
                    msg.Clear();
                }
            }
        }

        private readonly StringBuilder msg = new StringBuilder();

        private void DoInit()
        {
            _ticksUntilStartup = 5;
            _init = true;
            _master = DetermineIfMaster();
            if (!_master)
            {
                var set = new DefinitionSet();
                EnergyWeapons.Definitions.Create(set);
                MyAPIGateway.Utilities.SendModMessage(MOD_MESSAGE_DEFINITION_CHANNEL,
                    MyAPIGateway.Utilities.SerializeToXML(set));
                return;
            }

            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_MESSAGE_PING_MASTER_CHANNEL, MasterPingHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_MESSAGE_DEFINITION_CHANNEL, MasterDefinitionHandler);
            Scheduler = new UpdateScheduler();

            ComponentRegistry = new EntityComponentRegistry();
            ComponentRegistry.Register<ICoreRefComponent>((x) => x.OnAddedToCore(this),
                (x) => x.OnBeforeRemovedFromCore());
            ComponentRegistry.RegisterWithList<IRenderableComponent>();
            ComponentRegistry.RegisterComponentFactory(MakeAmmoGenerator);
            ComponentRegistry.RegisterComponentFactory(WeaponFactory<LaserWeaponComponent, LaserWeaponDefinition>);
            ComponentRegistry.RegisterComponentFactory((x) =>
            {
                if (x.Components.Get<NetworkController>() != null) return null;
                if (x is IMyCubeGrid) return new NetworkController(this);
                return null;
            });
            ComponentRegistry.RegisterComponentFactory((x) =>
            {
                var def = Definitions.BeamOf(x);
                if (def != null)
                    return new NetworkComponent(this);
                return null;
            });
            Definitions = new DefinitionManager();
            Materials = new MaterialPropertyDatabase();
            Physics = new ThermalPhysicsController(this);
            Logger.Info($"Initialized");

            {
                var set = new DefinitionSet();
                EnergyWeapons.Definitions.Create(set);
                Definitions.Add(set);
            }
        }

        private void MasterDefinitionHandler(object o1)
        {
            if (_ticksUntilStartup <= 0)
                MyLog.Default.Error($"Failed to register definitions, we've expired");

            var data = MyAPIGateway.Utilities.SerializeFromXML<DefinitionSet>((string) o1);
            Definitions.Add(data);
        }

        private void MasterPingHandler(object o1)
        {
            Logger.Info($"Recieved ping: {o1}");
            MyAPIGateway.Utilities.SendModMessage(MOD_MESSAGE_PING_SLAVE_CHANNEL, $"PingSlaveFrom {GetHashCode()}");
        }

        public bool DetermineIfMaster()
        {
            bool hasMaster = false;
            var slaveResponse = new Action<object>(o => hasMaster = true);
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_MESSAGE_PING_SLAVE_CHANNEL, slaveResponse);
            MyAPIGateway.Utilities.SendModMessage(MOD_MESSAGE_PING_MASTER_CHANNEL, $"PingMasterFrom {GetHashCode()}");
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_MESSAGE_PING_SLAVE_CHANNEL, slaveResponse);
            return !hasMaster;
        }

        private MyEntityComponentBase WeaponFactory<TComp, TDef>(IMyEntity ent)
            where TComp : WeaponComponent<TDef>, new() where TDef : EnergyWeaponDefinition
        {
            var def = Definitions.EnergyOf(ent);
            if (def is TDef)
            {
                Logger.Info($"Creating {typeof(TComp).Name} for {ent}");
                return new TComp();
            }
            else
                return null;
        }

        private MyEntityComponentBase MakeAmmoGenerator(IMyEntity ent)
        {
            var def = Definitions.EnergyOf(ent);
            if (def?.GenerateAmmo ?? false)
            {
                Logger.Info($"Creating {typeof(AmmoGeneratorComponent).Name} for {ent}");
                return new AmmoGeneratorComponent();
            }

            return null;
        }

        public override void SaveData()
        {
            if (!_master)
                return;
            Logger.Flush();
        }

        protected override void UnloadData()
        {
            if (!_master)
                return;
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_MESSAGE_PING_MASTER_CHANNEL, MasterPingHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_MESSAGE_DEFINITION_CHANNEL, MasterDefinitionHandler);
            ComponentRegistry.Detach();
            Logger.Detach();
            _loggerStatic = null;
            _init = false;
        }

        public override void Draw()
        {
            if (!_master)
                return;
            if (MyAPIGateway.Utilities.IsDedicated || ComponentRegistry == null)
                return;
            foreach (var render in ComponentRegistry.ComponentsOfType<IRenderableComponent>())
            {
                render.Draw();
                if (MyAPIGateway.Input.IsKeyPress(MyKeys.OemOpenBrackets))
                render.DebugDraw();
            }
        }
    }
}