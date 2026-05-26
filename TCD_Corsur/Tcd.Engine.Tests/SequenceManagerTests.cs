using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public class SequenceManagerTests
  {
    private static DelegateSequence MakeSeq(string key, string displayName = null) =>
      new DelegateSequence(key, displayName ?? key, (ctx, p, ct) => Task.CompletedTask);

    [Fact]
    public void Register_ValidSequence_ContainsByKey()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_A"));

      Assert.True(manager.Contains("SEQ_A"));
    }

    [Fact]
    public void Register_NullSequence_ThrowsArgumentNullException()
    {
      var manager = new SequenceManager();

      Assert.Throws<ArgumentNullException>(() => manager.Register(null));
    }

    [Fact]
    public void Register_DuplicateKey_ReplacesExistingSequence()
    {
      var manager = new SequenceManager();
      var first = new DelegateSequence("KEY", "First",
        (ctx, p, ct) => throw new InvalidOperationException("first"));
      var second = new DelegateSequence("KEY", "Second",
        (ctx, p, ct) => Task.CompletedTask);

      manager.Register(first);
      manager.Register(second);

      var list = manager.List();
      Assert.Single(list);
      foreach (var seq in list)
        Assert.Equal("Second", seq.DisplayName);
    }

    [Fact]
    public void Contains_UnregisteredKey_ReturnsFalse()
    {
      var manager = new SequenceManager();

      Assert.False(manager.Contains("UNKNOWN"));
    }

    [Fact]
    public void Contains_RegisteredKey_ReturnsTrue()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_B"));

      Assert.True(manager.Contains("SEQ_B"));
    }

    [Fact]
    public void List_EmptyManager_ReturnsEmptyCollection()
    {
      var manager = new SequenceManager();

      Assert.Empty(manager.List());
    }

    [Fact]
    public void List_AfterTwoRegistrations_ReturnsBothSequences()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_X"));
      manager.Register(MakeSeq("SEQ_Y"));

      Assert.Equal(2, manager.List().Count);
    }

    [Fact]
    public async Task RunAsync_NullKey_ThrowsArgumentNullException()
    {
      var manager = new SequenceManager();
      var ctx = new FakeSequenceContext();

      await Assert.ThrowsAsync<ArgumentNullException>(
        () => manager.RunAsync(null, ctx, null, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_NullContext_ThrowsArgumentNullException()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_C"));

      await Assert.ThrowsAsync<ArgumentNullException>(
        () => manager.RunAsync("SEQ_C", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_UnknownKey_ThrowsKeyNotFoundException()
    {
      var manager = new SequenceManager();
      var ctx = new FakeSequenceContext();

      await Assert.ThrowsAsync<KeyNotFoundException>(
        () => manager.RunAsync("NOT_EXIST", ctx, null, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_ReturnsSucceededForSuccessSequence()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_OK"));
      var ctx = new FakeSequenceContext();

      var result = await manager.RunAsync("SEQ_OK", ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task RunAsync_FiresStartedTraceEvent()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_T"));
      var ctx = new FakeSequenceContext();
      SequenceTraceEventArgs startedEvent = null;
      manager.Trace += (s, e) =>
      {
        if (e.Kind == SequenceTraceKind.Started)
          startedEvent = e;
      };

      await manager.RunAsync("SEQ_T", ctx, null, CancellationToken.None);

      Assert.NotNull(startedEvent);
      Assert.Equal(SequenceTraceKind.Started, startedEvent.Kind);
    }

    [Fact]
    public async Task RunAsync_FiresCompletedTraceEventWithResult()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("SEQ_U"));
      var ctx = new FakeSequenceContext();
      SequenceTraceEventArgs completedEvent = null;
      manager.Trace += (s, e) =>
      {
        if (e.Kind == SequenceTraceKind.Completed)
          completedEvent = e;
      };

      await manager.RunAsync("SEQ_U", ctx, null, CancellationToken.None);

      Assert.NotNull(completedEvent);
      Assert.Equal(SequenceTraceKind.Completed, completedEvent.Kind);
      Assert.Equal(SequenceStatus.Succeeded, completedEvent.Status);
    }

    [Fact]
    public async Task RunAsync_KeyLookupIsCaseInsensitive()
    {
      var manager = new SequenceManager();
      manager.Register(MakeSeq("seq_lower"));
      var ctx = new FakeSequenceContext();

      var result = await manager.RunAsync("SEQ_LOWER", ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
    }
  }
}
