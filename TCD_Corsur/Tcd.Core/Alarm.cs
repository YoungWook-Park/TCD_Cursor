using System;

namespace Tcd.Core
{
    public enum AlarmSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public sealed class Alarm
    {
        public Alarm(string code, string message, AlarmSeverity severity, DateTimeOffset timestamp)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
            Timestamp = timestamp;
        }

        public string Code { get; }
        public string Message { get; }
        public AlarmSeverity Severity { get; }
        public DateTimeOffset Timestamp { get; }

        public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Severity} {Code}: {Message}";
    }
}

