using System;
using System.Collections.Generic;
using ParallelTasks;
using VRage.Game.ModAPI;

namespace Equinox.Utils.Misc
{
    public static class ParallelExtensions
    {
        public static Task ForEachNonBlocking<T>(this IMyParallelTask parallel, IEnumerable<T> enumerable,
            Action<T> act)
        {
            var work = ForEachWork<T>.Get();
            work.Prepare(act, enumerable.GetEnumerator());
            return parallel.Start(work);
        }

        public static Task ForNonBlocking(this IMyParallelTask parallel, int minInclusive, int maxExclusive,
            Action<int> act, int stride = 1)
        {
            var work = ForLoopWork.Get();
            work.Prepare(minInclusive, maxExclusive, stride, act);
            return parallel.Start(work);
        }

        private class ForEachWork<T> : IWork
        {
            public static ForEachWork<T> Get()
            {
                var res = ObjectPool<ForEachWork<T>>.Singleton.BorrowUntracked();
                res._returned = false;
                return res;
            }

            public WorkOptions Options { get; private set; }

            public ForEachWork()
            {
                Options = new WorkOptions
                {
                    MaximumThreads = int.MaxValue
                };
            }

            public void Prepare(Action<T> act, IEnumerator<T> items)
            {
                _action = act;
                _enumerator = items;
                _notDone = true;
            }

            public void DoWork(WorkData workData = null)
            {
                T obj = default(T);
                while (_notDone)
                {
                    lock (this)
                    {
                        _notDone = _enumerator.MoveNext();
                        if (!_notDone)
                            break;
                        obj = _enumerator.Current;
                    }
                    _action(obj);
                }

                lock (this)
                {
                    if (!_returned)
                        Return();
                }
            }

            private void Return()
            {
                _enumerator?.Dispose();
                _enumerator = null;
                _action = null;
                _returned = true;
                ObjectPool<ForEachWork<T>>.Singleton.ReturnUntracked(this);
            }

            private Action<T> _action;
            private IEnumerator<T> _enumerator;
            private volatile bool _notDone;
            private volatile bool _returned;
        }

        private class ForLoopWork : IWork
        {
            public static ForLoopWork Get()
            {
                var res = ObjectPool<ForLoopWork>.Singleton.BorrowUntracked();
                res._returned = false;
                return res;
            }

            public WorkOptions Options { get; private set; }

            public ForLoopWork()
            {
                Options = new WorkOptions
                {
                    MaximumThreads = int.MaxValue
                };
            }

            private int _index;
            private int _max, _stride;
            private Action<int> _action;
            private bool _returned;


            public void Prepare(int min, int max, int stride, Action<int> action)
            {
                _index = min;
                _max = max;
                _stride = Math.Max(1, stride);
                _action = action;
            }

            public void DoWork(WorkData workData = null)
            {
                while (_index < _max)
                {
                    int exec;
                    lock (this)
                    {
                        exec = _index;
                        _index += _stride;
                    }

                    if (exec < _max)
                        _action(exec);
                }

                lock (this)
                {
                    if (!_returned)
                        Return();
                }
            }

            private void Return()
            {
                _returned = true;
                _action = null;
                ObjectPool<ForLoopWork>.Singleton.ReturnUntracked(this);
            }
        }
    }
}