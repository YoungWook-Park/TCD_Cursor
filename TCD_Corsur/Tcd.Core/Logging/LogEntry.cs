using System;
using System.Collections.Generic;

namespace Tcd.Core.Logging
{
    public sealed class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string SequenceKey { get; set; }
        public Guid? RunId { get; set; }
        public string StepName { get; set; }
        public string AxisName { get; set; }
        public string Message { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
        public string ExceptionMessage { get; set; }

        public LogEntry()
        {
            SequenceKey = "";
            StepName = "";
            AxisName = "";
            Message = "";
            Data = null;
            ExceptionMessage = "";
        }
    }
}
