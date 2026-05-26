using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class ActionNodeTests
  {
    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new ActionNode(null, "name", (ctx, ct) => Task.CompletedTask));
    }

    [Fact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new ActionNode("id", "name", null));
    }

    [Fact]
    public async Task RunAsync_ActionSucceeds_ReturnsSucceeded()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.CompletedTask);

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_ActionThrowsOperationCanceled_ReturnsStopped()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.FromException(new OperationCanceledException()));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task RunAsync_ActionThrowsException_ReturnsFailed()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.FromException(new InvalidOperationException("boom")));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
    }

    [Fact]
    public async Task RunAsync_OnException_RaisesAlarm_WithSEQ_ERROR_Code()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.FromException(new InvalidOperationException("boom")));

      await node.RunAsync(ctx, CancellationToken.None);

      Assert.Single(ctx.Alarms.Raised);
      Assert.Equal("SEQ_ERROR", ctx.Alarms.Raised[0].Code);
    }

    [Fact]
    public async Task RunAsync_OnTimeoutException_RaisesAlarm_WithSEQ_TIMEOUT_Code()
    {
      var ctx = new FakeSequenceContext();
      Func<ISequenceContext, CancellationToken, Task> neverEnding =
        (c, ct) => Task.Delay(Timeout.InfiniteTimeSpan, ct);
      var node = new ActionNode("id", "name", neverEnding,
        TimeSpan.FromMilliseconds(1));

      await node.RunAsync(ctx, CancellationToken.None);

      Assert.Single(ctx.Alarms.Raised);
      Assert.Equal("SEQ_TIMEOUT", ctx.Alarms.Raised[0].Code);
    }

    [Fact]
    public async Task RunAsync_WithNoTimeout_ActionSucceeds_ReturnsSucceeded()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.CompletedTask, timeout: null);

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_CompletesBeforeDeadline_Succeeds()
    {
      var ctx = new FakeSequenceContext();
      var node = new ActionNode("id", "name",
        (c, ct) => Task.CompletedTask,
        TimeSpan.FromSeconds(5));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ExceedsDeadline_RaisesAlarmAndFails()
    {
      var ctx = new FakeSequenceContext();
      Func<ISequenceContext, CancellationToken, Task> neverEnding =
        (c, ct) => Task.Delay(Timeout.InfiniteTimeSpan, ct);
      var node = new ActionNode("id", "name", neverEnding,
        TimeSpan.FromMilliseconds(1));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
      Assert.Single(ctx.Alarms.Raised);
      Assert.Equal("SEQ_TIMEOUT", ctx.Alarms.Raised[0].Code);
    }
  }
}
