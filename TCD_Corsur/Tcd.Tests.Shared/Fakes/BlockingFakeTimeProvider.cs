using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;

namespace Tcd.Tests.Shared.Fakes
{
  public sealed class BlockingFakeTimeProvider : ITimeProvider
  {
    private TaskCompletionSource<int> _tcs =
      new TaskCompletionSource<int>();

    public DateTimeOffset Now { get; set; } =
      new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
      return _tcs.Task;
    }

    public void Release()
    {
      _tcs.TrySetResult(0);
    }

    public void Reset()
    {
      _tcs = new TaskCompletionSource<int>();
    }
  }
}
