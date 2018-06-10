using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Equinox.EnergyWeapons.Components;
using Equinox.EnergyWeapons.Components.Beam;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Session;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class EnergyWeaponsCore : RegisteredSessionComponent
    {
        private const long MOD_MESSAGE_PING_MASTER_CHANNEL = 442403541L;
        private const long MOD_MESSAGE_PING_SLAVE_CHANNEL = MOD_MESSAGE_PING_MASTER_CHANNEL + 1;
        private const long MOD_MESSAGE_DEFINITION_CHANNEL = MOD_MESSAGE_PING_MASTER_CHANNEL + 2;
        
        private static CustomLogger _loggerStatic;
        public CustomLogger Logger => _loggerStatic;
        public static CustomLogger LoggerStatic => _loggerStatic;

        public bool Master { get; private set; }
        public DefinitionManager Definitions { get; private set; }

        public EnergyWeaponsCore() : base(typeof(EnergyWeaponsCore))
        {
        }

        public override void LoadData()
        {
            base.LoadData();
            Master = DetermineIfMaster();
            if (!Master)
            {
                var set = new DefinitionSet();
                EnergyWeapons.Definitions.Create(set);
                MyAPIGateway.Utilities.SendModMessage(MOD_MESSAGE_DEFINITION_CHANNEL,
                    MyAPIGateway.Utilities.SerializeToXML(set));
                return;
            }

            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_MESSAGE_PING_MASTER_CHANNEL, MasterPingHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_MESSAGE_DEFINITION_CHANNEL, MasterDefinitionHandler);

            _loggerStatic = new CustomLogger();

            Definitions = new DefinitionManager();
            {
                var set = new DefinitionSet();
                EnergyWeapons.Definitions.Create(set);
                Definitions.Add(set);
            }

            MyAPIGateway.Entities.OnEntityNameSet += CheckEntityComponents;
            MyVisualScriptLogicProvider.ItemSpawned += ItemSpawned;
            Logger.Info($"Initialized");
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            if (!Master)
                return;
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_MESSAGE_PING_MASTER_CHANNEL, MasterPingHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_MESSAGE_DEFINITION_CHANNEL, MasterDefinitionHandler);
            // ReSharper disable once DelegateSubtraction
            MyVisualScriptLogicProvider.ItemSpawned -= ItemSpawned;

            Logger.Detach();
            _loggerStatic = null;

            MyAPIGateway.Entities.OnEntityNameSet -= CheckEntityComponents;
        }

        public override void UpdateAfterSimulation()
        {
            if (!Master)
                return;
            Logger.UpdateAfterSimulation();
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.O))
                {
                    var caster = MyAPIGateway.Session.LocalHumanPlayer?.Character?.EquippedTool?.Components
                        .Get<MyCasterComponent>();
                    IMySlimBlock block = caster?.HitBlock;
                    if (block != null)
                    {
                        var tm = block?.FatBlock as IMyTerminalBlock;
                        if (tm != null)
                            _msg.Append("Block ").AppendLine(tm.CustomName);
                        else
                            _msg.Append("Slim ").AppendLine(block.Position.ToString());
                        var phys = MyAPIGateway.Session.GetComponent<ThermalManager>().PhysicsFor(block, false);
                        if (phys != null && phys.DebugWithType(_msg))
                            _msg.AppendLine();

                        if (block.FatBlock != null)
                        {
                            var k = new List<MyComponentBase>();
                            foreach (var c in block.FatBlock.Components)
                                k.Add(c);
                            foreach (var c in k)
                            {
                                var component = c as IDebugComponent;
                                if (component != null && !(component is ThermalPhysicsComponent) &&
                                    component.DebugWithType(_msg))
                                    _msg.AppendLine();
                            }
                        }

                        Logger.Info(_msg);
                        MyAPIGateway.Utilities.ShowNotification(_msg.ToString());
                        _msg.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Uncaught main thread error\n" + e.ToString());
            }
        }

        private readonly StringBuilder _msg = new StringBuilder();
        private readonly HashSet<MyDefinitionId> _generatedAmmos = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer); 

        private void CheckEntityComponents(IMyEntity e, string arg2, string arg3)
        {
            if (e is IMyCubeGrid)
            {
                if (!e.Components.Has<BeamController>())
                    e.Components.Add(new BeamController(this));
            }

            var def = Definitions.BeamOf(e);
            if (def != null)
            {
                if (def.RequiresResourceSink)
                {
                    if (!e.Components.Has<MyResourceSinkComponent>())
                    {
                        var sink = new MyResourceSinkComponent();
                        sink.Init(MyStringHash.GetOrCompute("Defense"), new List<MyResourceSinkInfo>());
                        e.Components.Add(sink);
                    }

                    if (!e.Components.Has<AdvancedResourceSink>())
                    {
                        var advSink = new AdvancedResourceSink(this);
                        e.Components.Add(advSink);
                    }
                }

                if (def.Components.OfType<Lossy>().Any())
                    e.Components.Add(new ThermalPhysicsComponent());

                if (def.Components.OfType<Weapon>().Any() && e is IMyUserControllableGun &&
                    !e.Components.Has<AmmoGeneratorComponent>())
                {
                    var wepDep = WeaponShortcuts.GetWeaponDefinition(e);
                    if (wepDep != null)
                        foreach (var k in wepDep.AmmoMagazinesId)
                            _generatedAmmos.Add(k);
                    e.Components.Add(new AmmoGeneratorComponent());
                }

                if (!e.Components.Has<NetworkComponent>())
                    e.Components.Add(new NetworkComponent(this));
            }
        }
        
        private void ItemSpawned(string itemtypename, string itemsubtypename, long itemid, int amount, Vector3D position)
        {
            MyObjectBuilderType type;
            if (!MyObjectBuilderType.TryParse(itemtypename, out type))
                return;
            var id = new MyDefinitionId(type, itemsubtypename);
            if (!_generatedAmmos.Contains(id))
                return;
            var ent = MyAPIGateway.Entities.GetEntityById(itemid);
            ent?.Close();
        }

        private void MasterDefinitionHandler(object o1)
        {
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
        
        public override void SaveData()
        {
            if (!Master)
                return;
            Logger.Flush();
        }
    }
}