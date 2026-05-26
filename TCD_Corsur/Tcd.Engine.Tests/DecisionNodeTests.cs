using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class DecisionNodeTests
  {
    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new DecisionNode(null, "name",
          (ctx, ct) => Task.FromResult(true)));
    }

    [Fact]
    public void Constructor_NullPredicate_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new DecisionNode("id", "name", null));
    }

    [Fact]
    public async Task RunAsync_PredicateReturnsTrue_ReturnsSucceeded()
    {
      var ctx = new FakeSequenceContext();
      var node = new DecisionNode("id", "name",
        (c, ct) => Task.FromResult(true));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_PredicateReturnsFalse_ReturnsFailed()
    {
      var ctx = new FakeSequenceContext();
      var node = new DecisionNode("id", "name",
        (c, ct) => Task.FromResult(false));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
    }

    [Fact]
    public async Task RunAsync_PredicateThrowsOperationCanceled_ReturnsStopped()
    {
      var ctx = new FakeSequenceContext();
      var node = new DecisionNode("id", "name",
        (c, ct) => Task.FromException<bool>(new OperationCanceledException()));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task RunAsync_PredicateThrowsException_RaisesAlarmAndReturnsFailed()
    {
      var ctx = new FakeSequenceContext();
      var node = new DecisionNode("id", "name",
        (c, ct) => Task.FromException<bool>(
          new InvalidOperationException("sensor error")));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
      Assert.Single(ctx.Alarms.Raised);
      Assert.Equal("SEQ_ERROR", ctx.Alarms.Raised[0].Code);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_CompletesBeforeDeadline_Succeeds()
    {
      var ctx = new FakeSequenceContext();
      var node = new DecisionNode("id", "name",
        (c, ct) => Task.FromResult(true),
        TimeSpan.FromSeconds(5));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ExceedsDeadline_RaisesAlarmAndFails()
    {
      var ctx = new FakeSequenceContext();
      Func<ISequenceContext, CancellationToken, Task<bool>> neverEnding =
        async (c, ct) =>
        {
          await Task.Delay(Timeout.InfiniteTimeSpan, ct);
          return true;
        };
      var node = new DecisionNode("id", "name", neverEnding,
        TimeSpan.FromMilliseconds(1));

      var result = await node.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
      Assert.Single(ctx.Alarms.Raised);
      Assert.Equal("SEQ_TIMEOUT", ctx.Alarms.Raised[0].Code);
    }
  }
}
