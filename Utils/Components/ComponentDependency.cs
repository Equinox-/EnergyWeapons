using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.ModAPI;

namespace Equinox.Utils.Components
{
    public class ComponentDependency<T> where T : MyEntityComponentBase
    {
        private readonly MyEntityComponentBase _owner;
        private readonly Func<IMyEntity, object, T> _factory;

        public event Action<T, T> ValueChanged;

        private readonly object _userData;
        private T _value;

        public T Value
        {
            get
            {
                if (_value == null && _factory != null && _owner.Entity != null && _owner.Container != null)
                {
                    _value = _factory(_owner.Entity, _userData);
                    if (_value != null)
                    {
                        _owner.Container.Add(typeof(T), _value);
                        ValueChanged?.Invoke(null, _value);
                    }
                }

                return _value;
            }
        }

        public ComponentDependency(MyEntityComponentBase owner, Func<IMyEntity, T> factory = null)
        {
            _owner = owner;
            if (factory != null)
                _factory = (a, b) => factory(a);
            else
                _factory = null;
            _userData = null;
        }

        public static ComponentDependency<T> DependencyWithFactory<TU>(MyEntityComponentBase owner, TU userData,
            Func<IMyEntity, TU, T> factory)
        {
            return new ComponentDependency<T>(owner, userData, (a, b) => factory(a, (TU) b));
        }

        private ComponentDependency(MyEntityComponentBase owner, object userData,
            Func<IMyEntity, object, T> factory = null)
        {
            _owner = owner;
            _factory = factory;
            _userData = userData;
        }

        public void OnAddedToContainer()
        {
            if (_owner.Container == null)
                return;
            _owner.Container.ComponentAdded += OnComponentAdded;
            _owner.Container.ComponentRemoved += OnComponentRemoved;
            foreach (var k in _owner.Container)
                OnComponentAdded(null, k as MyEntityComponentBase);
        }

        private void OnComponentAdded(Type type, MyEntityComponentBase ec)
        {
            var value = (ec as T) ?? (ec as MyCompositeGameLogicComponent)?.GetAs<T>();
            if (value != null)
            {
                var old = _value;
                _value = value;
                ValueChanged?.Invoke(old, _value);
            }
        }

        private void OnComponentRemoved(Type type, MyEntityComponentBase ec)
        {
            if (ec == Value)
            {
                var old = _value;
                _value = null;
                ValueChanged?.Invoke(old, _value);
            }
        }

        public void OnBeforeRemovedFromContainer()
        {
            var old = _value;
            _value = null;
            ValueChanged?.Invoke(old, _value);
            if (_owner.Container != null)
            {
                _owner.Container.ComponentAdded -= OnComponentAdded;
                _owner.Container.ComponentRemoved -= OnComponentRemoved;
            }
        }

        public static implicit operator T(ComponentDependency<T> val)
        {
            return val.Value;
        }
    }
}