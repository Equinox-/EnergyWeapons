using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Beam.Logic;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkComponent : MyEntityComponentBase
    {
        public readonly EnergyWeaponsCore Core;
        public NetworkController Controller { get; private set; }
        private readonly ILogging _log;

        public NetworkComponent(EnergyWeaponsCore core)
        {
            Core = core;
            _log = core.Logger.CreateProxy(GetType());
        }

        public override string ComponentTypeDebugString => nameof(NetworkComponent);
        private readonly List<DummyData> _dummies = new List<DummyData>();
        private readonly List<IComponent> _components = new List<IComponent>();

        public IReadOnlyList<DummyData> Dummies => _dummies;

        public override void OnAddedToScene()
        {
            var grid = (Entity as IMyCubeBlock)?.CubeGrid;
            if (grid == null)
            {
                _log.Warning("no grid");
                return;
            }

            Controller = grid.Components.Get<NetworkController>();
            if (Controller == null)
                grid.Components.Add(Controller = new NetworkController(Core));

            _dummies.Clear();
            _components.Clear();
            try
            {
                var def = Core.Definitions.BeamOf(Entity);
                foreach (var c in def)
                {
                    foreach (var d in c.Inputs.Concat(c.Outputs).Concat(c.Internal))
                    {
                        bool created;
                        var result = Controller.GetOrCreate(Entity, d, out created);
                        if (created)
                            _dummies.Add(result);
                    }

                    var cPath = c as Path;
                    if (cPath != null)
                    {
                        for (var i = 0; i < cPath.Dummies.Length - 1; i++)
                        {
                            Controller.Link(Entity, cPath.Dummies[i], Entity, cPath.Dummies[i + 1], cPath.Bidirectional, 1,
                                Vector4.One);
                        }
                    }

                    var cOptics = c as Optics;
                    if (cOptics != null)
                    {
                        foreach (var k in cOptics.IncomingBeams)
                            Controller.Link(Entity, k, Entity, cOptics.IntersectionPoint, false, 1, Vector4.One);
                        foreach (var k in cOptics.OutgoingBeams)
                            Controller.Link(Entity, cOptics.IntersectionPoint, Entity, k.Dummy, false, k.PowerFactor,
                                k.Color);
                    }

                    var cEmitter = c as Definition.Beam.Emitter;
                    if (cEmitter != null)
                    {
                        var k = new Logic.Emitter(this, cEmitter);
                        k.OnAddedToScene();
                        _components.Add(k);
                    }
                }

                foreach (var c in def.InputDetectors)
                    Controller.AddDetector(Entity, c, true, false);

                foreach (var c in def.OutputDetectors)
                    Controller.AddDetector(Entity, c, false, true);

                foreach (var c in def.BidirectionalDetectors)
                    Controller.AddDetector(Entity, c, true, true);
            }
            catch (Exception e)
            {
                EnergyWeaponsCore.LoggerStatic?.Error(e.ToString());
                Controller.DumpData();
                EnergyWeaponsCore.LoggerStatic?.Flush();
            }
        }

        public override void OnRemovedFromScene()
        {
            if (Controller == null)
                return;

            foreach (var k in _components)
                k.OnRemovedFromScene();
            _components.Clear();

            try
            {
                var def = Core.Definitions.BeamOf(Entity);
                foreach (var c in def)
                foreach (var key in c.Inputs.Concat(c.Outputs).Concat(c.Internal))
                        Controller.Remove(Entity, key);
            }
            catch (Exception e)
            {
                EnergyWeaponsCore.LoggerStatic?.Error(e.ToString());
                Controller.DumpData();
                EnergyWeaponsCore.LoggerStatic?.Flush();
            }

            _dummies.Clear();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene)
                OnRemovedFromScene();
        }

        public override void OnAddedToContainer()
        {
            if (Entity.InScene)
                OnAddedToScene();
        }
    }
}