using System;

namespace Tcd.Core.Logging
{
    /// <summary>
    /// Generic log context (no reference to ISequence). Optional sequence key, run id, axis name.
    /// </summary>
    public struct LogContext
    {
        public string SequenceKey { get; set; }
        public Guid? RunId { get; set; }
        public string AxisName { get; set; }

        public static LogContext Empty => new LogContext();
    }
}
