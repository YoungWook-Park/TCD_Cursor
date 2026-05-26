using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tcd.Core;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class AlarmManagerTests
  {
    [Fact]
    public void Raise_ValidAlarm_AppearsInSnapshot()
    {
      var manager = new AlarmManager();
      var alarm = new Alarm("E001", "Test error", AlarmSeverity.Error, DateTimeOffset.Now);

      manager.Raise(alarm);

      var snapshot = manager.Snapshot();
      Assert.Contains(alarm, snapshot);
    }

    [Fact]
    public void Raise_NullAlarm_ThrowsArgumentNullException()
    {
      var manager = new AlarmManager();

      Assert.Throws<ArgumentNullException>(() => manager.Raise((Alarm)null));
    }

    [Fact]
    public void Raise_FiresAlarmRaisedEvent()
    {
      var manager = new AlarmManager();
      var alarm = new Alarm("E001", "Test error", AlarmSeverity.Error, DateTimeOffset.Now);
      var eventFired = false;
      manager.AlarmRaised += (_, _) => eventFired = true;

      manager.Raise(alarm);

      Assert.True(eventFired);
    }

    [Fact]
    public void Raise_EventArg_IsSameInstanceAsRaisedAlarm()
    {
      var manager = new AlarmManager();
      var alarm = new Alarm("E001", "Test error", AlarmSeverity.Error, DateTimeOffset.Now);
      Alarm receivedAlarm = null;
      manager.AlarmRaised += (_, a) => receivedAlarm = a;

      manager.Raise(alarm);

      Assert.Same(alarm, receivedAlarm);
    }

    [Fact]
    public void Snapshot_BeforeAnyRaise_ReturnsEmptyList()
    {
      var manager = new AlarmManager();

      var snapshot = manager.Snapshot();

      Assert.Empty(snapshot);
    }

    [Fact]
    public void Snapshot_ReturnsIsolatedCopy_ExternalModifyDoesNotAffect()
    {
      var manager = new AlarmManager();
      var alarm = new Alarm("E001", "Test error", AlarmSeverity.Error, DateTimeOffset.Now);
      manager.Raise(alarm);

      var snapshot = (Alarm[])manager.Snapshot();
      snapshot[0] = null;

      var secondSnapshot = manager.Snapshot();
      Assert.Contains(alarm, secondSnapshot);
    }

    [Fact]
    public void Raise_MultipleAlarms_AllAccumulateInSnapshot()
    {
      var manager = new AlarmManager();
      var alarm1 = new Alarm("E001", "First error", AlarmSeverity.Error, DateTimeOffset.Now);
      var alarm2 = new Alarm("W001", "A warning", AlarmSeverity.Warning, DateTimeOffset.Now);
      var alarm3 = new Alarm("I001", "Info message", AlarmSeverity.Info, DateTimeOffset.Now);

      manager.Raise(alarm1);
      manager.Raise(alarm2);
      manager.Raise(alarm3);

      var snapshot = manager.Snapshot();
      Assert.Equal(3, snapshot.Count);
      Assert.Contains(alarm1, snapshot);
      Assert.Contains(alarm2, snapshot);
      Assert.Contains(alarm3, snapshot);
    }

    [Fact]
    public void Raise_StringOverload_PopulatesCodeAndMessage()
    {
      var manager = new AlarmManager();

      manager.Raise("E002", "Motor fault", AlarmSeverity.Error);

      var snapshot = manager.Snapshot();
      Assert.Single(snapshot);
      Assert.Equal("E002", snapshot[0].Code);
      Assert.Equal("Motor fault", snapshot[0].Message);
      Assert.Equal(AlarmSeverity.Error, snapshot[0].Severity);
    }

    [Fact]
    public async Task Raise_ConcurrentCalls_AllAlarmsAppearInSnapshot()
    {
      var manager = new AlarmManager();
      const int threadCount = 10;
      const int alarmsPerThread = 50;
      var tasks = new Task[threadCount];

      for (var i = 0; i < threadCount; i++)
      {
        var threadIndex = i;
        tasks[threadIndex] = Task.Run(() =>
        {
          for (var j = 0; j < alarmsPerThread; j++)
          {
            manager.Raise(
              new Alarm(
                $"E{threadIndex:D2}{j:D3}",
                "Concurrent alarm",
                AlarmSeverity.Error,
                DateTimeOffset.Now));
          }
        });
      }

      await Task.WhenAll(tasks);

      var snapshot = manager.Snapshot();
      Assert.Equal(threadCount * alarmsPerThread, snapshot.Count);
    }
  }
}
