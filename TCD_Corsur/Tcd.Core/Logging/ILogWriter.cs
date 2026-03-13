using System;
using System.Collections.Generic;
using System.Threading;

namespace Tcd.Core.Logging
{
    /// <summary>
    /// Async, non-blocking log writer. Callers enqueue only; no file I/O on caller thread.
    /// </summary>
    public interface ILogWriter
    {
        void Log(LogLevel level, in LogContext ctx, string stepName, string message,
            IReadOnlyDictionary<string, object> data = null, Exception ex = null);

        void Trace(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null);
        void Debug(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null);
        void Info(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null);
        void Warn(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null);
        void Error(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null, Exception ex = null);
        void Fatal(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null, Exception ex = null);
    }
}
