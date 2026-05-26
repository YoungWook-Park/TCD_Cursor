using System.Collections.Generic;
using Tcd.Core;

namespace Tcd.Tests.Shared.Fakes
{
  public sealed class FakeAlarmSink : IAlarmSink
  {
    public List<Alarm> Raised { get; } = new List<Alarm>();

    public void Raise(Alarm alarm)
    {
      Raised.Add(alarm);
    }
  }
}
