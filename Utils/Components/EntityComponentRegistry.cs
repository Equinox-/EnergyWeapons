using System;
using System.Collections.Generic;
using System.Linq;
using Equinox.EnergyWeapons;
using Equinox.Utils.Logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Entity.EntityComponents.Interfaces;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Equinox.Utils.Components
{
    public class EntityComponentRegistry
    {
        private readonly Dictionary<Type, Action<MyEntityComponentBase>> _callbacksOnAdd =
            new Dictionary<Type, Action<MyEntityComponentBase>>();

        private readonly Dictionary<Type, Action<MyEntityComponentBase>> _callbacksOnRemove =
            new Dictionary<Type, Action<MyEntityComponentBase>>();

        private readonly Dictionary<Type, IComponentList> _componentLists = new Dictionary<Type, IComponentList>();

        private readonly HashSet<Func<IMyEntity, MyEntityComponentBase>> _componentFactory =
            new HashSet<Func<IMyEntity, MyEntityComponentBase>>();

        private interface IComponentList
        {
            void Add(MyEntityComponentBase ec);
            void Remove(MyEntityComponentBase ec);
            IEnumerable<MyEntityComponentBase> Enumerable { get; }
        }

        private class ComponentList<T> : IComponentList where T : class
        {
            private readonly HashSet<T> _entries = new HashSet<T>();


            public void Add(MyEntityComponentBase ec)
            {
                var test = ec as T;
                if (test != null)
                    _entries.Add(test);
            }

            public void Remove(MyEntityComponentBase ec)
            {
                var test = ec as T;
                if (test != null)
                    _entries.Remove(test);
            }

            public IEnumerable<MyEntityComponentBase> Enumerable
            {
                get { return _entries.OfType<MyEntityComponentBase>(); }
            }
        }

        public void Register<T>(Action<T> onAdd, Action<T> onRemove) where T : class
        {
            _callbacksOnAdd.Add(typeof(T), (x) =>
            {
                var res = x as T;
                if (res != null)
                    onAdd.Invoke(res);
            });
            _callbacksOnRemove.Add(typeof(T), (x) =>
            {
                var res = x as T;
                if (res != null)
                    onRemove.Invoke(res);
            });
            MyAPIGateway.Entities.GetEntities(null, (x) =>
            {
                foreach (var c in x.Components)
                {
                    var ec = c as MyEntityComponentBase;
                    if (ec != null)
                        DoComponentCalls(ec, onAdd);
                }

                return false;
            });
        }

        public void RegisterWithList<T>() where T : class
        {
            var list = new ComponentList<T>();
            _componentLists.Add(typeof(T), list);
            MyAPIGateway.Entities.GetEntities(null, (x) =>
            {
                foreach (var c in x.Components)
                {
                    var ec = c as MyEntityComponentBase;
                    if (ec != null)
                        DoComponentCalls<MyEntityComponentBase>(ec, list.Add);
                }

                return false;
            });
        }

        public void RegisterComponentFactory(Func<IMyEntity, MyEntityComponentBase> factory)
        {
            _componentFactory.Add(factory);
        }

        public void UnregisterComponentFactory(Func<IMyEntity, MyEntityComponentBase> factory)
        {
            _componentFactory.Remove(factory);
        }

        public void Unregister<T>()
        {
            var list = _componentLists.GetValueOrDefault(typeof(T));
            var onRemove = _callbacksOnRemove.GetValueOrDefault(typeof(T));
            _callbacksOnAdd.Remove(typeof(T));
            _callbacksOnRemove.Remove(typeof(T));
            _componentLists.Remove(typeof(T));
            MyAPIGateway.Entities.GetEntities(null, (x) =>
            {
                foreach (var c in x.Components)
                {
                    var ec = c as MyEntityComponentBase;
                    if (ec != null)
                        DoComponentCalls(ec, (MyEntityComponentBase eb) =>
                        {
                            onRemove?.Invoke(eb);
                            list?.Remove(eb);
                        });
                }

                return false;
            });
        }

        public IEnumerable<T> ComponentsOfType<T>()
        {
            return _componentLists[typeof(T)].Enumerable.OfType<T>();
        }

        public void Attach()
        {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            MyAPIGateway.Entities.GetEntities(null, (x) =>
            {
                OnEntityAdd(x);
                return false;
            });
        }

        public void Detach()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            MyAPIGateway.Entities.GetEntities(null, (x) =>
            {
                OnEntityRemove(x);
                return false;
            });
        }

        private void OnEntityAdd(IMyEntity e)
        {
            var grid = e as IMyCubeGrid;
            if (grid != null)
            {
                grid.GetBlocks(null, (x) =>
                {
                    OnBlockAdded(x);
                    return false;
                });
                grid.OnBlockAdded += OnBlockAdded;
                grid.OnBlockRemoved += OnBlockRemoved;
            }

            if (e.Components == null)
                return;

            List<MyGameLogicComponent> gameLogic = null;
            foreach (var k in _componentFactory)
            {
                var res = k(e);
                if (res == null)
                    continue;
                var gl = res as MyGameLogicComponent;
                if (gl != null)
                {
                    if (gameLogic == null)
                    {
                        gameLogic = new List<MyGameLogicComponent>();
                        var glInit = e.GameLogic as MyGameLogicComponent;
                        if (glInit != null && !(e.GameLogic is MyNullGameLogicComponent))
                            gameLogic.Add(glInit);
                    }

                    gameLogic.Add(gl);
                    continue;
                }

                e.Components.Add(res.GetType(), res);
            }

            if (gameLogic != null)
                e.GameLogic = MyCompositeGameLogicComponent.Create(gameLogic, (MyEntity) e);

            e.Components.ComponentAdded += OnComponentAdd;
            e.Components.ComponentRemoved += OnComponentRemoved;
            foreach (var c in e.Components)
            {
                var ec = c as MyEntityComponentBase;
                if (ec != null)
                    OnComponentAdd(null, ec);
            }
        }
        
        private void OnEntityRemove(IMyEntity e)
        {
            var grid = e as IMyCubeGrid;
            if (grid != null)
            {
                grid.GetBlocks(null, (x) =>
                {
                    OnBlockRemoved(x);
                    return false;
                });
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockRemoved -= OnBlockRemoved;
            }

            if (e.Components == null)
                return;

            e.Components.ComponentAdded -= OnComponentAdd;
            e.Components.ComponentRemoved -= OnComponentRemoved;
            foreach (var c in e.Components)
            {
                var ec = c as MyEntityComponentBase;
                if (ec != null)
                    OnComponentRemoved(null, ec);
            }
        }

        private void OnBlockAdded(IMySlimBlock obj)
        {
            if (obj.FatBlock != null)
                OnEntityAdd(obj.FatBlock);
        }


        private void OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj.FatBlock != null)
                OnEntityRemove(obj.FatBlock);
        }


        private void OnComponentAdd(Type ignore, MyEntityComponentBase c)
        {
            DoCompositeCalls(c, OnComponentAdd);

            foreach (var k in _callbacksOnAdd.Values)
                k.Invoke(c);
            foreach (var k in _componentLists.Values)
                k.Add(c);
        }

        private void OnComponentRemoved(Type ignore, MyEntityComponentBase c)
        {
            DoCompositeCalls(c, OnComponentRemoved);

            foreach (var k in _callbacksOnRemove.Values)
                k.Invoke(c);
            foreach (var k in _componentLists.Values)
                k.Remove(c);
        }

        private void DoComponentCalls<T>(MyEntityComponentBase c, Action<T> call) where T : class
        {
            if (c == null)
                return;
            Action<Type, MyEntityComponentBase> lam = (a, b) =>
            {
                var tb = b as T;
                if (tb != null)
                    call(tb);
            };
            lam(null, c);
            DoCompositeCalls(c, lam);
        }

        private void DoCompositeCalls(MyEntityComponentBase c, Action<Type, MyEntityComponentBase> act)
        {
            var aggregate = c as IMyComponentAggregate;
            if (aggregate != null)
                foreach (var ch in aggregate.ChildList.Reader)
                {
                    var ec = ch as MyEntityComponentBase;
                    if (ec != null)
                        act.Invoke(null, ec);
                }

            var comp = c as MyCompositeGameLogicComponent;
            if (comp == null)
                return;
            var kf = comp.GetAs<MyCompositeGameLogicComponent>();
            if (kf != null)
                DoCompositeCalls(kf, act);
            foreach (var t in GameLogicTypes.Types)
            {
                var r = comp.GetAs(t);
                if (r != null)
                    act.Invoke(null, r);
            }
        }
    }
}