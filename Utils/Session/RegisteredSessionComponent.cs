using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Equinox.Utils.Session
{
    public abstract class RegisteredSessionComponent : MySessionComponentBase
    {
        internal readonly Type[] RegisteredTypes;

        protected RegisteredSessionComponent(params Type[] registered)
        {
            RegisteredTypes = registered;
        }

        public override void LoadData()
        {
            base.LoadData();
            RegisteredSessionComponentsExt.Register(this);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            RegisteredSessionComponentsExt.Unregister(this);
        }
    }

    public static class RegisteredSessionComponentsExt
    {
        private static readonly Dictionary<IMySession, Dictionary<Type, RegisteredSessionComponent>> _storage =
            new Dictionary<IMySession, Dictionary<Type, RegisteredSessionComponent>>();

        internal static void Register(RegisteredSessionComponent s)
        {
            if (s.Session == null)
                return;
            lock (_storage)
            {
                Dictionary<Type, RegisteredSessionComponent> dat;
                if (!_storage.TryGetValue(s.Session, out dat))
                    _storage.Add(s.Session, dat = new Dictionary<Type, RegisteredSessionComponent>());
                foreach (var k in s.RegisteredTypes)
                    dat[k] = s;
            }
        }

        internal static void Unregister(RegisteredSessionComponent s)
        {
            if (s.Session == null)
                return;
            lock (_storage)
            {
                Dictionary<Type, RegisteredSessionComponent> dat;
                if (_storage.TryGetValue(s.Session, out dat))
                {
                    foreach (var k in s.RegisteredTypes)
                        if (dat.GetValueOrDefault(k) == s)
                            dat.Remove(k);
                    if (dat.Count == 0)
                        _storage.Remove(s.Session);
                }
            }
        }

        public static T GetComponent<T>(this IMySession session) where T : RegisteredSessionComponent
        {
            lock (_storage)
            {
//                return _storage.GetValueOrDefault(session)?.GetValueOrDefault(typeof(T)) as T;
                var tmp = _storage.GetValueOrDefault(session);
                if (tmp == null)
                    return null;
                lock (tmp)
                {
                    var tmp2 = tmp.GetValueOrDefault(typeof(T));
                    if (tmp2 == null)
                        return null;
                    lock (tmp2)
                    {
                        return tmp2 as T;
                    }
                }
            }
        }
    }
}