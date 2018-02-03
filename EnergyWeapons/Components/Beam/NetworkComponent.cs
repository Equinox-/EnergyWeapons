using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Beam.Logic;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment,
        Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkComponent : MyEntityComponentBase, IDebugComponent, IRenderableComponent
    {
        public readonly EnergyWeaponsCore Core;
        public BeamController Controller { get; private set; }
        private readonly ILogging _log;

        public NetworkComponent(EnergyWeaponsCore core)
        {
            Core = core;
            _log = core.Logger.CreateProxy(GetType());
        }

        public override string ComponentTypeDebugString => nameof(NetworkComponent);
        private readonly List<DummyData> _dummies = new List<DummyData>();
        private readonly List<IComponent> _components = new List<IComponent>();
        private readonly List<IRenderableComponent> _renderable = new List<IRenderableComponent>();

        public IReadOnlyList<DummyData> Dummies => _dummies;

        public override void OnAddedToScene()
        {
            var grid = (Entity as IMyCubeBlock)?.CubeGrid;
            if (grid == null)
            {
                _log.Warning("no grid");
                return;
            }

            Controller = grid.Components.Get<BeamController>();
            if (Controller == null)
                grid.Components.Add(Controller = new BeamController(Core));

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
                            Controller.Link(Entity, cPath.Dummies[i], Entity, cPath.Dummies[i + 1],
                                new BeamConnectionData(Vector4.One, float.PositiveInfinity, cPath.Bidirectional));
                        }
                    }

                    var cOptics = c as Optics;
                    if (cOptics != null)
                    {
                        foreach (var k in cOptics.IncomingBeams)
                            Controller.Link(Entity, k, Entity, cOptics.IntersectionPoint,
                                new BeamConnectionData(Vector4.One, float.PositiveInfinity, false));
                        foreach (var k in cOptics.OutgoingBeams)
                            Controller.Link(Entity, cOptics.IntersectionPoint, Entity, k.Dummy,
                                new BeamConnectionData(k.Color, k.MaxThroughput, false));
                    }

                    var cEmitter = c as Definition.Beam.Emitter;
                    if (cEmitter != null)
                    {
                        var k = new Logic.Emitter(this, cEmitter);
                        k.OnAddedToScene();
                        _components.Add(k);
                    }

                    var cWeapon = c as Definition.Beam.Weapon;
                    if (cWeapon != null)
                    {
                        var k = new Logic.Weapon(this, cWeapon);
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

                _renderable.Clear();
                _renderable.AddRange(_components.OfType<IRenderableComponent>());
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
            _renderable.Clear();
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

        public void Draw()
        {
            foreach (var x in _renderable)
                x.Draw();
        }

        public void DebugDraw()
        {
            foreach (var x in _renderable)
                x.DebugDraw();
        }

        public void Debug(StringBuilder sb)
        {
            const string indent = "  ";
            const string childTag = "Children:\n";

            sb.Append("Network: ").AppendLine(Dummies.Count.ToString());
            foreach (var l in Dummies.Select(x => x.Segment).Where(x => x != null).Distinct())
            {
                sb.Append(indent).Append("Seg ").Append(l.GetHashCode().ToString("X8")).Append(" ");
                l.Debug(sb);
                sb.AppendLine();
            }

            var child = false;
            sb.Append(childTag);
            foreach (var x in _components.OfType<IDebugComponent>())
            {
                sb.Append(indent);
                if (!x.DebugWithType(sb))
                    sb.Remove(sb.Length - indent.Length, indent.Length);
                else
                {
                    child = true;
                    sb.AppendLine();
                }
            }

            if (!child)
                sb.Remove(sb.Length - childTag.Length, childTag.Length);
            sb.Remove(sb.Length - 1, 1); // nl
        }
    }
}