using System;
using System.Collections.Generic;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Session;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;

namespace Equinox.EnergyWeapons.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ThermalManager : RegisteredSessionComponent
    {
        private static readonly TimeSpan _cleanupPeriod = TimeSpan.FromSeconds(10);

        private EnergyWeaponsCore _core;

        private readonly MyConcurrentPool<ThermalPhysicsTemporary>
            _thermalPhysicsPool = new MyConcurrentPool<ThermalPhysicsTemporary>(32);

        private readonly List<KeyValuePair<object, ThermalPhysicsTemporary>> _physicsForUpdate =
            new List<KeyValuePair<object, ThermalPhysicsTemporary>>();

        private readonly Dictionary<object, ThermalPhysicsSlim> _physicsForEntry =
            new Dictionary<object, ThermalPhysicsSlim>();

        public override Type[] Dependencies { get; } = {typeof(EnergyWeaponsCore)};

        public MaterialPropertyDatabase Materials { get; private set; }


        public ThermalManager() : base(typeof(ThermalManager))
        {
        }

        public override void LoadData()
        {
            base.LoadData();
            _core = Session.GetComponent<EnergyWeaponsCore>();
            if (_core == null)
                throw new Exception("No core component!");
            _core.Logger.CreateProxy(GetType());

            Materials = new MaterialPropertyDatabase();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            _core = null;
        }

        private int _updateOffset = 0;
        private const int UpdateSpread = 10;

        public override void UpdateAfterSimulation()
        {
            if (_core == null || !_core.Master)
                return;
            var expiry = MyAPIGateway.Session.ElapsedPlayTime - _cleanupPeriod;
            for (var i = _updateOffset; i < _physicsForUpdate.Count; i += UpdateSpread)
            {
                while (i < _physicsForUpdate.Count)
                {
                    var entry = _physicsForUpdate[i];
                    var destroyable = entry.Key as IMyDestroyableObject;
                    var entity = entry.Key as IMyEntity;
                    if ((destroyable == null || destroyable.Integrity > 0) &&
                        (entity == null || (entity.InScene && !entity.Closed)) && entry.Value.LastUsed >= expiry)
                        break;
                    _physicsForEntry.Remove(entry.Key);
                    _thermalPhysicsPool.Return(entry.Value);
                    _physicsForUpdate.RemoveAtFast(i);
                }

                if (i >= _physicsForUpdate.Count)
                    continue;

                var target = _physicsForUpdate[i].Key;
                var destroy = target as IMyDestroyableObject ?? (target as IMyCubeBlock)?.SlimBlock;
                _physicsForUpdate[i].Value.Update(destroy);
            }

            _updateOffset = (_updateOffset + 1) % UpdateSpread;
        }

        public ThermalPhysicsSlim PhysicsFor(IMySlimBlock block, bool allocate = true)
        {
            if (block.FatBlock != null)
            {
                var k = PhysicsFor(block.FatBlock, false);
                if (k != null)
                    return k;
            }

            ThermalPhysicsSlim phys;
            if (_physicsForEntry.TryGetValue(block, out phys))
            {
                var temp = phys as ThermalPhysicsTemporary;
                if (temp != null)
                    temp.LastUsed = MyAPIGateway.Session.ElapsedPlayTime;
                return phys;
            }

            if (!allocate)
                return null;

            return _physicsForEntry[block] = AllocateTemp(block);
        }

        public ThermalPhysicsSlim PhysicsFor(IMyEntity ent, bool allocate = true)
        {
            ThermalPhysicsSlim phys;
            if (_physicsForEntry.TryGetValue(ent, out phys))
            {
                var temp = phys as ThermalPhysicsTemporary;
                if (temp != null)
                    temp.LastUsed = MyAPIGateway.Session.ElapsedPlayTime;
                return phys;
            }

            if (!allocate)
                return null;

            var block = ent as IMyCubeBlock;
            if (block != null)
                return PhysicsFor(block.SlimBlock);

            if (ent is IMyDestroyableObject)
            {
                var res = AllocateTemp(ent);
                if (res != null)
                    return _physicsForEntry[ent] = res;
            }

            return null;
        }

        private ThermalPhysicsTemporary AllocateTemp(object o)
        {
            var t = _thermalPhysicsPool.Get();
            t.LastUsed = MyAPIGateway.Session.ElapsedPlayTime;
            var block = o as IMySlimBlock;
            var ent = o as IMyEntity;
            if ((block != null && t.Init(Materials, block)) || (ent != null && t.Init(Materials, ent)))
            {
                _physicsForUpdate.Add(new KeyValuePair<object, ThermalPhysicsTemporary>(o, t));
                return t;
            }

            _thermalPhysicsPool.Return(t);
            return null;
        }

        private class ThermalPhysicsTemporary : ThermalPhysicsSlim
        {
            public TimeSpan LastUsed;
        }
    }
}