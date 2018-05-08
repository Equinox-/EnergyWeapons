using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Equinox.EnergyWeapons.Components.Beam.Logic;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using VRage.Game.ModAPI;
using VRageMath;
using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment,
        Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkComponent : ComponentSceneCallback, IDebugComponent, IRenderableComponent
    {
        public readonly EnergyWeaponsCore Core;
        public BeamController Controller { get; private set; }
        public readonly ComponentDependency<AdvancedResourceSink> ResourceSink;
        private readonly ILogging _log;

        public NetworkComponent(EnergyWeaponsCore core)
        {
            Core = core;
            _log = core.Logger.CreateProxy(GetType());
            ResourceSink = new ComponentDependency<AdvancedResourceSink>(this);
        }

        public override string ComponentTypeDebugString => nameof(NetworkComponent);
        private readonly List<DummyData> _dummies = new List<DummyData>();
        private readonly List<IComponent> _components = new List<IComponent>();
        private readonly List<IRenderableComponent> _renderable = new List<IRenderableComponent>();

        private IMyCubeBlock Block => (IMyCubeBlock) Entity;

        public IReadOnlyList<DummyData> Dummies => _dummies;

        public override void OnAddedToScene()
        {
            if (!Entity.IsPhysicallyPresent())
                return;
            base.OnAddedToScene();
            if (Block.IsWorking)
                RegisterNetwork();
            Block.IsWorkingChanged += IsWorkingChanged;
        }

        private void IsWorkingChanged(IMyCubeBlock myCubeBlock)
        {
            if (myCubeBlock.IsWorking)
                RegisterNetwork();
            else
                UnregisterNetwork();
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
            Block.IsWorkingChanged -= IsWorkingChanged;
            UnregisterNetwork();
        }

        private void RegisterNetwork()
        {
            if (Controller != null)
            {
                _log.Warning("Already registered...");
                return;
            }

            Controller = (Entity as IMyCubeBlock)?.CubeGrid.Components.Get<BeamController>();
            if (Controller == null)
            {
                _log.Warning("no grid controller");
                return;
            }

            _dummies.Clear();
            _components.Clear();
            try
            {
                var def = Core.Definitions.BeamOf(Entity);
                foreach (var c in def.Components)
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
                                new BeamConnectionData(Vector4.One));
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
                        _components.Add(k);
                    }

                    var cWeapon = c as Definition.Beam.Weapon;
                    if (cWeapon != null)
                    {
                        var k = new Logic.Weapon(this, cWeapon);
                        _components.Add(k);
                    }
                }

                foreach (var c in def.InputDetectors)
                    Controller.AddDetector(Entity, c, true, false);

                foreach (var c in def.OutputDetectors)
                    Controller.AddDetector(Entity, c, false, true);

                foreach (var c in def.BidirectionalDetectors)
                    Controller.AddDetector(Entity, c, true, true);


                foreach (var k in _components)
                    k.OnAddedToScene();

                _renderable.Clear();
                _renderable.AddRange(_components.OfType<IRenderableComponent>());

                this.RegisterRenderable();
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
                Controller.DumpData();
            }
        }

        private void UnregisterNetwork()
        {
            if (Controller == null)
                return;

            foreach (var k in _components)
                k.OnRemovedFromScene();
            _components.Clear();

            try
            {
                var def = Core.Definitions.BeamOf(Entity);
                foreach (var c in def.Components)
                foreach (var key in c.Inputs.Concat(c.Outputs).Concat(c.Internal))
                    Controller.Remove(Entity, key);
                this.UnregisterRenderable();
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
                Controller.DumpData();
            }

            _dummies.Clear();
            _renderable.Clear();
            Controller = null;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            ResourceSink.OnBeforeRemovedFromContainer();
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            ResourceSink.OnAddedToContainer();
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