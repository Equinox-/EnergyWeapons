using System;
using System.Collections.Concurrent;

namespace Equinox.Utils.Misc
{
    public class ObjectPool<T> where T : class, new()
    {
        private static ObjectPool<T> _singleton;

        public static ObjectPool<T> Singleton => _singleton ?? (_singleton = new ObjectPool<T>(5));

        private readonly ConcurrentQueue<T> _cache = new ConcurrentQueue<T>();
        private readonly int _limit;


        public ObjectPool(int limit)
        {
            _limit = limit;
        }

        public T BorrowUntracked()
        {
            T res;
            return _cache.TryDequeue(out res) ? res : new T();
        }

        public void ReturnUntracked(T v)
        {
            if (_cache.Count > _limit)
                return;
            _cache.Enqueue(v);
        }

        public Token BorrowTracked()
        {
            return new Token(this);
        }

        public struct Token : IDisposable
        {
            public T Value { get; private set; }

            private readonly ObjectPool<T> _pool;

            public Token(ObjectPool<T> pool)
            {
                _pool = pool;
                Value = _pool.BorrowUntracked();
            }

            public static implicit operator T(Token t)
            {
                return t.Value;
            }

            public void Dispose()
            {
                _pool.ReturnUntracked(Value);
                Value = null;
            }
        }
    }
}