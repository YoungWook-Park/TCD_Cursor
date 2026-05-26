using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public class DelegateSequenceTests
  {
    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new DelegateSequence(null, "display", (ctx, p, ct) => Task.CompletedTask));
    }

    [Fact]
    public void Constructor_NullBody_ThrowsArgumentNullException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new DelegateSequence("KEY", "display", null));
    }

    [Fact]
    public void Constructor_NullDisplayName_DefaultsToKey()
    {
      var seq = new DelegateSequence("MY_KEY", null, (ctx, p, ct) => Task.CompletedTask);
      Assert.Equal("MY_KEY", seq.DisplayName);
    }

    [Fact]
    public void Key_And_DisplayName_ReturnConstructorValues()
    {
      var seq = new DelegateSequence("SEQ_KEY", "My Display", (ctx, p, ct) => Task.CompletedTask);
      Assert.Equal("SEQ_KEY", seq.Key);
      Assert.Equal("My Display", seq.DisplayName);
    }

    [Fact]
    public async Task ExecuteAsync_BodySucceeds_ReturnsSucceeded()
    {
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => Task.CompletedTask);
      var ctx = new FakeSequenceContext();

      var result = await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceledException_ReturnsStopped()
    {
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => throw new OperationCanceledException());
      var ctx = new FakeSequenceContext();

      var result = await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_GeneralException_ReturnsFailed()
    {
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => throw new InvalidOperationException("oops"));
      var ctx = new FakeSequenceContext();

      var result = await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_OnException_RaisesAlarm()
    {
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => throw new InvalidOperationException("oops"));
      var ctx = new FakeSequenceContext();

      await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.NotEmpty(ctx.Alarms.Raised);
    }

    [Fact]
    public async Task ExecuteAsync_OnException_AlarmCode_IsSEQ_ERROR()
    {
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => throw new InvalidOperationException("oops"));
      var ctx = new FakeSequenceContext();

      await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.Equal("SEQ_ERROR", ctx.Alarms.Raised[0].Code);
    }

    [Fact]
    public async Task ExecuteAsync_OnException_ErrorMessageMatchesExceptionMessage()
    {
      var exMessage = "something went wrong";
      var seq = new DelegateSequence("KEY", "display",
        (ctx, p, ct) => throw new InvalidOperationException(exMessage));
      var ctx = new FakeSequenceContext();

      var result = await seq.ExecuteAsync(ctx, null, CancellationToken.None);

      Assert.Equal(exMessage, result.Error);
    }
  }
}
