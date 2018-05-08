using System;
using Sandbox.Game.Entities;
using VRage.Game.Components;

namespace Equinox.Utils.Components
{
    public class ComponentDependency<T> where T : MyEntityComponentBase
    {
        private readonly MyEntityComponentBase _owner;

        public event Action<T, T> ValueChanged;
        private T _value;

        public T Value => _value;

        public ComponentDependency(MyEntityComponentBase owner)
        {
            _owner = owner;
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