using System;

namespace Tcd.Core.Logging
{
    public struct LogContext
    {
        public string SequenceKey { get; set; }
        public Guid? RunId { get; set; }
        public string AxisName { get; set; }

        public static LogContext Empty => new LogContext();
    }
}
