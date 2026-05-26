using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;

namespace Tcd.Tests.Shared.Fakes
{
  public sealed class FakeTimeProvider : ITimeProvider
  {
    public DateTimeOffset Now { get; set; } =
      new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
      if (cancellationToken.IsCancellationRequested)
        return Task.FromCanceled(cancellationToken);
      return Task.CompletedTask;
    }
  }
}
