﻿using System;
using System.Text;
using VRage.Utils;

namespace Equinox.Utils.Logging
{
    public interface ILogging
    {
        MyLogSeverity Level { get; set; }

        void IncreaseIndent();

        void DecreaseIndent();

        void Log(MyLogSeverity severity, string message);

        void Log(MyLogSeverity severity, string format, params object[] args);

        void Log(MyLogSeverity severity, StringBuilder message);
    }

    public interface ILoggingBase : ILogging
    {
        void LogRoot(MyLogSeverity severity, string prefix, string message);
        void LogRoot(MyLogSeverity severity, string prefix, string format, params object[] args);
        void LogRoot(MyLogSeverity severity, string prefix, StringBuilder message);
        void Flush();
    }

    public static class LoggingExtension
    {
        public static ILoggingBase Root(this ILogging self)
        {
            var result = (self as ILoggingBase) ?? (self as LoggingProxy)?.Backing.Root();
            if (result == null)
                throw new Exception("No logging root");
            return result;
        }

        public static ILogging CreateProxy(this ILoggingBase self, string prefix, MyLogSeverity level)
        {
            return new LoggingProxy(self, prefix) {Level = level};
        }

        private struct IndentToken : IDisposable
        {
            private ILogging m_log;

            public IndentToken(ILogging logger)
            {
                m_log = logger;
                m_log.IncreaseIndent();
            }

            public void Dispose()
            {
                m_log.DecreaseIndent();
                m_log = null;
            }
        }

        public static IDisposable IndentUsing(this ILogging self)
        {
            return new IndentToken(self);
        }

        public static void Debug(this ILogging self, string message)
        {
            self.Log(MyLogSeverity.Debug, message);
        }

        public static void Debug(this ILogging self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Debug, message, args);
        }

        public static void Debug(this ILogging self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Debug, buillder);
        }

        public static void Info(this ILogging self, string message)
        {
            self.Log(MyLogSeverity.Info, message);
        }

        public static void Info(this ILogging self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Info, message, args);
        }

        public static void Info(this ILogging self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Info, buillder);
        }

        public static void Warning(this ILogging self, string message)
        {
            self.Log(MyLogSeverity.Warning, message);
        }

        public static void Warning(this ILogging self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Warning, message, args);
        }

        public static void Warning(this ILogging self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Warning, buillder);
        }

        public static void Error(this ILogging self, string message)
        {
            self.Log(MyLogSeverity.Error, message);
        }

        public static void Error(this ILogging self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Error, message, args);
        }

        public static void Error(this ILogging self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Error, buillder);
        }

        public static void Critical(this ILogging self, string message)
        {
            self.Log(MyLogSeverity.Critical, message);
        }

        public static void Critical(this ILogging self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Critical, message, args);
        }

        public static void Critical(this ILogging self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Critical, buillder);
        }
    }
}