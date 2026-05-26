using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Simulator;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Simulator.Tests
{
  public class SimAxisTests
  {
    [Fact]
    public void Constructor_NullName_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimAxis(null, new FakeTimeProvider()));
    }

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimAxis("U", null));
    }

    [Fact]
    public void Constructor_ZeroOrNegativeSpeed_DefaultsTo50()
    {
      var axis = new SimAxis("U", new FakeTimeProvider(), unitsPerSecond: 0);
      Assert.NotNull(axis);
    }

    [Fact]
    public void InitialPosition_IsZero()
    {
      var axis = new SimAxis("U", new FakeTimeProvider());
      Assert.Equal(0.0, axis.Position);
    }

    [Fact]
    public void IsMoving_Initial_IsFalse()
    {
      var axis = new SimAxis("U", new FakeTimeProvider());
      Assert.False(axis.IsMoving);
    }

    [Fact]
    public void IsServoOn_DefaultIsFalse()
    {
      var axis = new SimAxis("U", new FakeTimeProvider());
      Assert.False(axis.IsServoOn);
    }

    [Fact]
    public void IsServoOn_SetTrue_ReturnsTrue()
    {
      var axis = new SimAxis("U", new FakeTimeProvider());
      axis.IsServoOn = true;
      Assert.True(axis.IsServoOn);
    }

    [Fact]
    public async Task CommandMoveToAsync_WithFakeTime_PositionUpdatedToTarget()
    {
      var fakeTime = new FakeTimeProvider();
      var axis = new SimAxis("U", fakeTime, unitsPerSecond: 100);

      await axis.CommandMoveToAsync(50, CancellationToken.None);
      await axis.WaitForInPositionAsync(
        50, 0.01, TimeSpan.FromSeconds(1), CancellationToken.None);

      Assert.Equal(50.0, axis.Position, 2);
    }

    [Fact]
    public async Task CommandMoveToAsync_WithBlockingFake_IsMovingIsTrueWhileMoving()
    {
      var blocking = new BlockingFakeTimeProvider();
      var axis = new SimAxis("U", blocking, unitsPerSecond: 100);

      _ = axis.CommandMoveToAsync(50, CancellationToken.None);
      await Task.Yield();

      Assert.True(axis.IsMoving);

      blocking.Release();
    }

    [Fact]
    public async Task CommandMoveToAsync_WithFakeTime_IsMovingIsFalseAfterComplete()
    {
      var fakeTime = new FakeTimeProvider();
      var axis = new SimAxis("U", fakeTime, unitsPerSecond: 100);

      await axis.CommandMoveToAsync(50, CancellationToken.None);
      await axis.WaitForInPositionAsync(
        50, 0.01, TimeSpan.FromSeconds(1), CancellationToken.None);

      Assert.False(axis.IsMoving);
    }

    [Fact]
    public async Task WaitForInPosition_AlreadyAtTarget_ReturnsImmediately()
    {
      var fakeTime = new FakeTimeProvider();
      var axis = new SimAxis("U", fakeTime);

      await axis.WaitForInPositionAsync(
        0, 0.01, TimeSpan.FromSeconds(1), CancellationToken.None);
    }

    [Fact]
    public async Task WaitForInPosition_WithFakeTime_AfterCommandMove_Succeeds()
    {
      var fakeTime = new FakeTimeProvider();
      var axis = new SimAxis("U", fakeTime, unitsPerSecond: 100);

      await axis.CommandMoveToAsync(75, CancellationToken.None);
      await axis.WaitForInPositionAsync(
        75, 0.01, TimeSpan.FromSeconds(1), CancellationToken.None);

      Assert.Equal(75.0, axis.Position, 2);
    }

    [Fact]
    public async Task WaitForInPosition_TimesOut_ThrowsTimeoutException()
    {
      var axis = new SimAxis("U", new SystemTimeProvider(), unitsPerSecond: 1);

      await Assert.ThrowsAsync<TimeoutException>(() =>
        axis.WaitForInPositionAsync(
          1000, 0.01, TimeSpan.FromMilliseconds(10), CancellationToken.None));
    }

    [Fact]
    public async Task WaitForInPosition_Cancellation_ThrowsOperationCanceledException()
    {
      var axis = new SimAxis("U", new SystemTimeProvider(), unitsPerSecond: 1);
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        axis.WaitForInPositionAsync(
          1000, 0.01, TimeSpan.FromSeconds(5), cts.Token));
    }
  }
}
