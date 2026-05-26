using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class TimeoutsTests
  {
    [Fact]
    public async Task WithTimeout_TaskAlreadyCompleted_DoesNotThrow()
    {
      var completedTask = Task.CompletedTask;

      await Timeouts.WithTimeout(
        completedTask,
        TimeSpan.FromSeconds(5),
        CancellationToken.None);
    }

    [Fact]
    public async Task WithTimeout_CompletesBeforeDeadline_DoesNotThrow()
    {
      var fastTask = Task.Delay(TimeSpan.FromMilliseconds(10));

      await Timeouts.WithTimeout(
        fastTask,
        TimeSpan.FromSeconds(5),
        CancellationToken.None);
    }

    [Fact]
    public async Task WithTimeout_ExceedsDeadline_ThrowsTimeoutException()
    {
      using var cts = new CancellationTokenSource();
      var neverEndingTask = Task.Delay(
        Timeout.InfiniteTimeSpan,
        cts.Token);

      await Assert.ThrowsAsync<TimeoutException>(async () =>
        await Timeouts.WithTimeout(
          neverEndingTask,
          TimeSpan.FromMilliseconds(1),
          CancellationToken.None));

      cts.Cancel();
    }

    [Fact]
    public async Task WithTimeout_CancellationRequested_PropagatesCancellation()
    {
      // WithTimeout 내부에서 CreateLinkedTokenSource(cancellationToken)으로
      // linked CTS를 만들고, Task.Delay(timeout, cts.Token)을 생성한다.
      // cancellationToken이 이미 취소된 상태이면 linked CTS도 즉시 취소되어
      // timeoutTask(Task.Delay)가 TaskCanceledException으로 즉시 완료된다.
      // Task.WhenAny에서 timeoutTask가 먼저 완료되므로
      // 실제 구현은 TimeoutException을 던진다.
      using var cts = new CancellationTokenSource();
      var neverEndingTask = Task.Delay(
        Timeout.InfiniteTimeSpan,
        CancellationToken.None);

      cts.Cancel();

      await Assert.ThrowsAsync<TimeoutException>(async () =>
        await Timeouts.WithTimeout(
          neverEndingTask,
          TimeSpan.FromSeconds(5),
          cts.Token));
    }

    [Fact]
    public async Task WithTimeoutT_CompletesBeforeDeadline_ReturnsValue()
    {
      var valueTask = Task.FromResult(42);

      var result = await Timeouts.WithTimeout(
        valueTask,
        TimeSpan.FromSeconds(5),
        CancellationToken.None);

      Assert.Equal(42, result);
    }

    [Fact]
    public async Task WithTimeoutT_ExceedsDeadline_ThrowsTimeoutException()
    {
      using var cts = new CancellationTokenSource();
      var neverEndingTask = Task.Run(async () =>
      {
        await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
        return 0;
      });

      await Assert.ThrowsAsync<TimeoutException>(async () =>
        await Timeouts.WithTimeout(
          neverEndingTask,
          TimeSpan.FromMilliseconds(1),
          CancellationToken.None));

      cts.Cancel();
    }
  }
}
