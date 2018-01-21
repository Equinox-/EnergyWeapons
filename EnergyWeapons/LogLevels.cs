using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Beam.Logic;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Logging;
using VRage.Utils;


namespace Equinox.EnergyWeapons
{
    public static class LogLevels
    {
        private static readonly Dictionary<string, MyLogSeverity> _levels = new Dictionary<string, MyLogSeverity>();
        
        private static void AddNamespace(Type t, MyLogSeverity s)
        {
            // ReSharper disable once PossibleNullReferenceException
            _levels.Add(t.FullName.Substring(0, t.FullName.LastIndexOf('.')), s);
        }

        private static void Add(Type t, MyLogSeverity s)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _levels.Add(t.FullName, s);
        }

        static LogLevels()
        {
            _levels.Add("", MyLogSeverity.Warning);
            AddNamespace(typeof(EnergyWeaponsCore), MyLogSeverity.Info);
            Add(typeof(Emitter), MyLogSeverity.Debug);
        }

        public static ILogging CreateProxy(this ILoggingBase logger, Type type)
        {
            var match = type.FullName ?? "";

            MyLogSeverity? result = null;
            while (match.Length > 0)
            {
                MyLogSeverity severity;
                if (_levels.TryGetValue(match, out severity))
                {
                    result = severity;
                    break;
                }

                var i = match.LastIndexOf('.');
                if (i < 0)
                    break;
                match = match.Substring(0, i);
            }
            return logger.CreateProxy(type.Name, result ?? _levels[""]);
        }
    }
}