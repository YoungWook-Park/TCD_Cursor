using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class SequenceRunnerTests
  {
    [Fact]
    public async Task Run_SingleSuccessNode_ReturnsSucceeded()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task Run_ChainedTwoNodes_BothExecute()
    {
      var executed = new List<string>();
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => { executed.Add("n1"); return Task.CompletedTask; }));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => { executed.Add("n2"); return Task.CompletedTask; }));
      graph.SetNext("n1", "n2");
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(new[] { "n1", "n2" }, executed);
    }

    [Fact]
    public async Task Run_ChainedThreeNodes_ExecuteInOrder()
    {
      var order = new List<string>();
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => { order.Add("n1"); return Task.CompletedTask; }));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => { order.Add("n2"); return Task.CompletedTask; }));
      graph.AddNode(new ActionNode("n3", "N3",
        (ctx, ct) => { order.Add("n3"); return Task.CompletedTask; }));
      graph.SetNext("n1", "n2");
      graph.SetNext("n2", "n3");
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(new[] { "n1", "n2", "n3" }, order);
    }

    [Fact]
    public async Task Run_FirstNodeFails_SecondNodeNotExecuted()
    {
      var executed = new List<string>();
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.FromException(new InvalidOperationException("fail"))));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => { executed.Add("n2"); return Task.CompletedTask; }));
      graph.SetNext("n1", "n2");
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Empty(executed);
    }

    [Fact]
    public async Task Run_FirstNodeFails_ReturnsFailedResult()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.FromException(new InvalidOperationException("fail"))));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => Task.CompletedTask));
      graph.SetNext("n1", "n2");
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Run_NodeReturnsStopped_PropagatesStopped()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.FromException(
          new OperationCanceledException())));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task Run_CancellationBeforeStart_ReturnsStopped()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(
        () => runner.RunAsync(ctx, cts.Token));
    }

    [Fact]
    public async Task Run_ForkNode_AllBranchesExecute()
    {
      var executed = new List<string>();
      var graph = new SequenceGraph("fork");
      var fork = new ForkNode("fork", "Fork",
        new[] { "b1", "b2" }, null);
      graph.AddNode(fork);
      graph.AddNode(new ActionNode("b1", "B1",
        (ctx, ct) => { executed.Add("b1"); return Task.CompletedTask; }));
      graph.AddNode(new ActionNode("b2", "B2",
        (ctx, ct) => { executed.Add("b2"); return Task.CompletedTask; }));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Contains("b1", executed);
      Assert.Contains("b2", executed);
      Assert.Equal(2, executed.Count);
    }

    [Fact]
    public async Task Run_ForkNode_AllBranchesSucceed_ContinuesToJoinNext()
    {
      var executed = new List<string>();
      var graph = new SequenceGraph("fork");
      var fork = new ForkNode("fork", "Fork",
        new[] { "b1", "b2" }, "join");
      graph.AddNode(fork);
      graph.AddNode(new ActionNode("b1", "B1",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("b2", "B2",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("join", "Join",
        (ctx, ct) => { executed.Add("join"); return Task.CompletedTask; }));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
      Assert.Contains("join", executed);
    }

    [Fact]
    public async Task Run_ForkNode_OneBranchFails_PropagatesFailure()
    {
      var graph = new SequenceGraph("fork");
      var fork = new ForkNode("fork", "Fork",
        new[] { "b1", "b2" }, "join");
      graph.AddNode(fork);
      graph.AddNode(new ActionNode("b1", "B1",
        (ctx, ct) => Task.FromException(
          new InvalidOperationException("branch fail"))));
      graph.AddNode(new ActionNode("b2", "B2",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("join", "Join",
        (ctx, ct) => Task.CompletedTask));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Run_ForkNode_JoinNextIsNull_StopsAfterJoin()
    {
      var afterFork = new List<string>();
      var graph = new SequenceGraph("fork");
      var fork = new ForkNode("fork", "Fork",
        new[] { "b1", "b2" }, null);
      graph.AddNode(fork);
      graph.AddNode(new ActionNode("b1", "B1",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("b2", "B2",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("next", "Next",
        (ctx, ct) => { afterFork.Add("next"); return Task.CompletedTask; }));
      graph.SetNext("fork", "next");
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
      Assert.Empty(afterFork);
    }

    [Fact]
    public async Task Run_NestedFork_AllBranchesSucceed_ReturnsSucceeded()
    {
      var graph = new SequenceGraph("outerFork");
      var outerFork = new ForkNode("outerFork", "OuterFork",
        new[] { "innerFork", "b3" }, null);
      var innerFork = new ForkNode("innerFork", "InnerFork",
        new[] { "b1", "b2" }, null);
      graph.AddNode(outerFork);
      graph.AddNode(innerFork);
      graph.AddNode(new ActionNode("b1", "B1",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("b2", "B2",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("b3", "B3",
        (ctx, ct) => Task.CompletedTask));
      var runner = new SequenceRunner(graph);
      var ctx = new FakeSequenceContext();

      var result = await runner.RunAsync(ctx, CancellationToken.None);

      Assert.Equal(NodeRunStatus.Succeeded, result.Status);
    }
  }
}
