using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Logging;
using Sandbox.Game.Entities.Character.Components;
using VRage;
using VRage.Collections;

namespace Equinox.Utils.Scheduler
{
    public class UpdateScheduler
    {
        public delegate void DelUpdate(ulong deltaTicks);

        private long _ticks;

        private struct ScheduledUpdate
        {
            public readonly DelUpdate Callback;
            public readonly long LastUpdate, NextUpdate;
            public readonly long Interval;

            public ScheduledUpdate(DelUpdate callback, long lastUpdate, long nextUpdate, long interval = -1)
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

        private readonly MyBinaryStructHeap<long, ScheduledUpdate> _scheduledUpdates = new MyBinaryStructHeap<long, ScheduledUpdate>();

        private readonly FastResourceLock _lock = new FastResourceLock();
        private readonly HashSet<DelUpdate> _updatesToRemove = new HashSet<DelUpdate>();
        private readonly List<ScheduledUpdate> _updatesToAdd = new List<ScheduledUpdate>();


        private long _intervalsScheduled;

        public void RepeatingUpdate(DelUpdate update, ulong interval, long delay = -1)
        {
            long nextUpdate;
            if (delay >= 0)
                nextUpdate = _ticks + delay;
            else
            {
                // kinda hacky way to spread updates out.  Works best when people use the same interval
                var block = (_intervalsScheduled++) % (long) interval;
                nextUpdate = (_ticks / (long) interval) * (long) interval + block;
            }

            EnergyWeapons.EnergyWeaponsCore.LoggerStatic?.Info(
                $"Repeating {update.Method} on {update.Target} interval={interval}, delay={delay}");
            using (_lock.AcquireExclusiveUsing())
                _updatesToAdd.Add(new ScheduledUpdate(update, nextUpdate - (long) interval, nextUpdate, (long) interval));
        }

        public void DelayedUpdate(DelUpdate update, ulong delay = 0)
        {
            EnergyWeapons.EnergyWeaponsCore.LoggerStatic?.Info(
                $"Invoking {update.Method} on {update.Target} delay={delay}");
            using (_lock.AcquireExclusiveUsing())
                _updatesToAdd.Add(new ScheduledUpdate(update, _ticks, _ticks + (long) delay));
        }

        public void RemoveUpdate(DelUpdate update)
        {
            EnergyWeapons.EnergyWeaponsCore.LoggerStatic?.Info($"Removing {update.Method} on {update.Target}");
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

        public void RunUpdate(long ticks)
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
                test.Callback((ulong) (_ticks - test.LastUpdate));
                if (test.Interval > 0)
                {
                    var next = new ScheduledUpdate(test.Callback, _ticks, test.NextUpdate + test.Interval, test.Interval);
                    _scheduledUpdates.Insert(next, next.NextUpdate);
                }
            } while (true);
        }
    }
}