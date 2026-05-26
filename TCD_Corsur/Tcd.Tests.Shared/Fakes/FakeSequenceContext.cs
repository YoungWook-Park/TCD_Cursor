using System.Threading;
using Tcd.Core;
using Tcd.Sequence;

namespace Tcd.Tests.Shared.Fakes
{
  public sealed class FakeSequenceContext : ISequenceContext
  {
    public FakeAlarmSink Alarms { get; } = new FakeAlarmSink();
    public FakeTimeProvider Time { get; } = new FakeTimeProvider();

    IAlarmSink ISequenceContext.Alarms => Alarms;
    ITimeProvider ISequenceContext.Time => Time;

    public CancellationToken StopToken { get; } = CancellationToken.None;
  }
}
