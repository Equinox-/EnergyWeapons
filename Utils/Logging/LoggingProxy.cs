using System.Text;
using VRage.Utils;

namespace Equinox.Utils.Logging
{
    public class LoggingProxy : ILogging
    {
        public readonly ILoggingBase Backing;
        public readonly string Prefix;
        public MyLogSeverity Level { get; set; }

        public LoggingProxy(ILoggingBase backing, string prefix)
        {
            Backing = backing;
            Prefix = prefix + " ";
        }

        // TODO these should probably only apply to the proxy.
        public void IncreaseIndent()
        {
            Backing.IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            Backing.DecreaseIndent();
        }

        public void Log(MyLogSeverity severity, string message)
        {
            if ((int)severity < (int)Level) return;
            Backing.LogRoot(severity, Prefix, message);
        }

        public void Log(MyLogSeverity severity, string format, params object[] args)
        {
            if ((int)severity < (int)Level) return;
            Backing.LogRoot(severity, Prefix, format, args);
        }

        public void Log(MyLogSeverity severity, StringBuilder message)
        {
            if ((int)severity < (int)Level) return;
            Backing.LogRoot(severity, Prefix, message);
        }
    }
}