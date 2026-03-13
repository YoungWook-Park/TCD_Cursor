using System;
using System.Collections.Generic;

namespace Tcd.Core
{
    public interface IAlarmSink
    {
        void Raise(Alarm alarm);
    }

    public sealed class AlarmManager : IAlarmSink
    {
        private readonly object _gate = new object();
        private readonly List<Alarm> _alarms = new List<Alarm>();

        public event EventHandler<Alarm> AlarmRaised;

        public IReadOnlyList<Alarm> Snapshot()
        {
            lock (_gate)
            {
                return _alarms.ToArray();
            }
        }

        public void Raise(Alarm alarm)
        {
            if (alarm == null) throw new ArgumentNullException(nameof(alarm));

            lock (_gate)
            {
                _alarms.Add(alarm);
            }

            AlarmRaised?.Invoke(this, alarm);
        }

        public void Raise(string code, string message, AlarmSeverity severity = AlarmSeverity.Error)
        {
            Raise(new Alarm(code, message, severity, DateTimeOffset.Now));
        }
    }
}

