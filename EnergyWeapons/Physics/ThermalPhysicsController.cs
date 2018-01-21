using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;

namespace Equinox.EnergyWeapons.Physics
{
    public class ThermalPhysicsController
    {
        private static readonly TimeSpan _cleanupPeriod =
            TimeSpan.FromSeconds(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 60);

        private readonly EnergyWeaponsCore _core;
        private readonly ILogging _log;

        private readonly MyConcurrentPool<ThermalPhysicsTemporary>
            _thermalPhysicsPool = new MyConcurrentPool<ThermalPhysicsTemporary>(32);

        public ThermalPhysicsController(EnergyWeaponsCore core)
        {
            _core = core;
            _log = core.Logger.CreateProxy(GetType());
            _core.ComponentRegistry.Register<ThermalPhysicsComponent>(x =>
                {
                    _componentEntityKey[x] = x.Entity;
                    _physicsForEntity[x.Entity] = x.Physics;
                },
                x =>
                {
                    var ent = _componentEntityKey.GetValueOrDefault(x);
                    _componentEntityKey.Remove(x);
                    if (ent != null)
                        _physicsForEntity.Remove(ent);
                });
        }

        private readonly List<KeyValuePair<object, ThermalPhysicsTemporary>> _physicsForUpdate =
            new List<KeyValuePair<object, ThermalPhysicsTemporary>>();

        private readonly Dictionary<IMySlimBlock, ThermalPhysicsSlim> _physicsForBlock =
            new Dictionary<IMySlimBlock, ThermalPhysicsSlim>();

        private readonly Dictionary<IMyEntity, ThermalPhysicsSlim> _physicsForEntity =
            new Dictionary<IMyEntity, ThermalPhysicsSlim>();

        private readonly Dictionary<ThermalPhysicsComponent, IMyEntity> _componentEntityKey =
            new Dictionary<ThermalPhysicsComponent, IMyEntity>();


        private int _updateOffset = 0;
        private const int _updateSpread = 10;

        public void Update()
        {
            var expiry = MyAPIGateway.Session.ElapsedPlayTime - _cleanupPeriod;
            for (var i = _updateOffset; i < _physicsForUpdate.Count; i += _updateSpread)
            {
                while (i < _physicsForUpdate.Count && _physicsForUpdate[i].Value.LastUsed < expiry)
                {
                    var dos = _physicsForUpdate[i].Key;
                    var slim = dos as IMySlimBlock;
                    if (slim != null)
                        _physicsForBlock.Remove(slim);
                    var ent = dos as IMyEntity;
                    if (ent != null)
                        _physicsForEntity.Remove(ent);
                    _thermalPhysicsPool.Return(_physicsForUpdate[i].Value);
                    _physicsForUpdate.RemoveAtFast(i);
                }

                if (i >= _physicsForUpdate.Count)
                    continue;
                _physicsForUpdate[i].Value.UpdateIndex = i;

                var target = _physicsForUpdate[i].Key;
                var destroy = target as IMyDestroyableObject ?? (target as IMyCubeBlock)?.SlimBlock;
                _physicsForUpdate[i].Value.Update(destroy);
            }

            _updateOffset = (_updateOffset + 1) % _updateSpread;
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
            if (_physicsForBlock.TryGetValue(block, out phys))
            {
                var temp = phys as ThermalPhysicsTemporary;
                if (temp != null)
                    temp.LastUsed = MyAPIGateway.Session.ElapsedPlayTime;
                return phys;
            }

            if (!allocate)
                return null;

            return _physicsForBlock[block] = AllocateTemp(block);
        }

        public ThermalPhysicsSlim PhysicsFor(IMyEntity ent, bool allocate = true)
        {
            ThermalPhysicsSlim phys;
            if (_physicsForEntity.TryGetValue(ent, out phys))
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
                    return _physicsForEntity[ent] = res;
            }

            return null;
        }

        private ThermalPhysicsTemporary AllocateTemp(object o)
        {
            var t = _thermalPhysicsPool.Get();
            t.UpdateIndex = _physicsForUpdate.Count;
            t.LastUsed = MyAPIGateway.Session.ElapsedPlayTime;
            var block = o as IMySlimBlock;
            var ent = o as IMyEntity;
            if ((block != null && t.Init(_core.Materials, block)) || (ent != null && t.Init(_core.Materials, ent)))
            {
                _physicsForUpdate.Add(new KeyValuePair<object, ThermalPhysicsTemporary>(o, t));
                return t;
            }

            _thermalPhysicsPool.Return(t);
            return null;
        }

        private class ThermalPhysicsTemporary : ThermalPhysicsSlim
        {
            public int UpdateIndex;
            public TimeSpan LastUsed;
        }
    }
}