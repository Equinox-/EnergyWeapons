using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Utils;

namespace Equinox.Utils.Logging
{
    public abstract class LoggerBase : ILoggingBase
    {
        private readonly StringBuilder _messageBuilder = new StringBuilder();
        private readonly StringBuilder _indentBuilder = new StringBuilder();

        public MyLogSeverity Level { get; set; } = (MyLogSeverity)0;

        protected abstract void Write(StringBuilder message);

        public abstract void Flush();

        public void IncreaseIndent()
        {
            lock (_messageBuilder)
            {
                _indentBuilder.Append("    ");
            }
        }

        public void DecreaseIndent()
        {
            lock (_messageBuilder)
            {
                if (_indentBuilder.Length >= 4)
                    _indentBuilder.Remove(_indentBuilder.Length - 4, 4);
            }
        }

        #region Root
        public void LogRoot(MyLogSeverity severity, string prefix, string format, params object[] args)
        {
            lock (_messageBuilder)
            {
                _messageBuilder.Append(prefix);
                _messageBuilder.Append(_indentBuilder);
                _messageBuilder.AppendFormat(" {0}: ", severity);
                _messageBuilder.AppendFormat(format, args);
                Write(_messageBuilder);
                _messageBuilder.Clear();
            }

            if (severity >= MyLogSeverity.Error)
                Flush();
        }

        public void LogRoot(MyLogSeverity severity, string prefix, string message)
        {
            lock (_messageBuilder)
            {
                _messageBuilder.Append(prefix);
                _messageBuilder.Append(_indentBuilder);
                _messageBuilder.AppendFormat("{0}: ", severity);
                _messageBuilder.Append(message);
                Write(_messageBuilder);
                _messageBuilder.Clear();
            }

            if (severity >= MyLogSeverity.Error)
                Flush();
        }

        public void LogRoot(MyLogSeverity severity, string prefix, StringBuilder message)
        {
            lock (_messageBuilder)
            {
                _messageBuilder.Append(prefix);
                _messageBuilder.Append(_indentBuilder);
                _messageBuilder.AppendFormat("{0}: ", severity);
                _messageBuilder.Append(message);
                Write(_messageBuilder);
                _messageBuilder.Clear();
            }

            if (severity >= MyLogSeverity.Error)
                Flush();
        }
#endregion

        public void Log(MyLogSeverity severity, string format, params object[] args)
        {
            if ((int)severity < (int)Level) return;
            LogRoot(severity, "", format, args);
        }

        public void Log(MyLogSeverity severity, StringBuilder message)
        {
            if ((int)severity < (int)Level) return;
            LogRoot(severity, "", message);
        }

        public void Log(MyLogSeverity severity, string message)
        {
            if ((int)severity < (int)Level) return;
            LogRoot(severity, "", message);
        }
    }
}
