using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Character.Components;
using VRage;
using VRage.Collections;

namespace Equinox.Utils.Scheduler
{
    public class UpdateScheduler
    {
        public delegate void DelUpdate(ulong deltaTicks);

        private ulong _ticks;

        private struct ScheduledUpdate
        {
            public readonly DelUpdate Callback;
            public readonly ulong LastUpdate, NextUpdate;
            public readonly long Interval;

            public ScheduledUpdate(DelUpdate callback, ulong lastUpdate, ulong nextUpdate, long interval = -1)
            {
                Callback = callback;
                LastUpdate = lastUpdate;
                NextUpdate = nextUpdate;
                Interval = interval;
            }
        }

        private class UpdateEquality : IEqualityComparer<ScheduledUpdate>
        {
            public static readonly UpdateEquality Instance = new UpdateEquality();

            public bool Equals(ScheduledUpdate x, ScheduledUpdate y)
            {
                return x.Callback == y.Callback;
            }

            public int GetHashCode(ScheduledUpdate obj)
            {
                return obj.Callback.GetHashCode();
            }
        }

        private readonly MyBinaryStructHeap<ulong, ScheduledUpdate> _scheduledUpdates =
            new MyBinaryStructHeap<ulong, ScheduledUpdate>();

        private readonly FastResourceLock _lock = new FastResourceLock();
        private readonly HashSet<DelUpdate> _updatesToRemove = new HashSet<DelUpdate>();
        private readonly List<ScheduledUpdate> _updatesToAdd = new List<ScheduledUpdate>();


        private ulong _intervalsScheduled;

        public void RepeatingUpdate(DelUpdate update, ulong interval, long delay = -1)
        {
            ulong nextUpdate;
            if (delay >= 0)
                nextUpdate = _ticks + (ulong) delay;
            else
            {
                // kinda hacky way to spread updates out.  Works best when people use the same interval
                var block = (_intervalsScheduled++) % (ulong) interval;
                nextUpdate = (_ticks / interval) * interval + block;
            }

            using (_lock.AcquireExclusiveUsing())
                _updatesToAdd.Add(new ScheduledUpdate(update, nextUpdate, nextUpdate - interval, (long) interval));
        }

        public void DelayedUpdate(DelUpdate update, ulong delay = 0)
        {
            using (_lock.AcquireExclusiveUsing())
                _updatesToAdd.Add(new ScheduledUpdate(update, _ticks, _ticks + delay));
        }

        public void RemoveUpdate(DelUpdate update)
        {
            using (_lock.AcquireExclusiveUsing())
                _updatesToRemove.Add(update);
        }

        private void ApplyChanges()
        {
            using (_lock.AcquireExclusiveUsing())
            {
                foreach (var x in _updatesToRemove)
                    _scheduledUpdates.Remove(new ScheduledUpdate(x, 0, 0), UpdateEquality.Instance);
                _updatesToRemove.Clear();
                foreach (var x in _updatesToAdd)
                    _scheduledUpdates.Insert(x, x.NextUpdate);
                _updatesToAdd.Clear();
            }
        }

        public void RunUpdate(ulong ticks)
        {
            ApplyChanges();
            _ticks += ticks;
            do
            {
                if (_scheduledUpdates.Count == 0)
                    return;
                if (_scheduledUpdates.MinKey() > _ticks)
                    return;
                var test = _scheduledUpdates.RemoveMin();
                test.Callback(_ticks - test.LastUpdate);
                if (test.Interval > 0)
                {
                    var next = new ScheduledUpdate(test.Callback, _ticks, test.NextUpdate + (ulong) test.Interval,
                        test.Interval);
                    _scheduledUpdates.Insert(next, next.NextUpdate);
                }
            } while (true);
        }
    }
}