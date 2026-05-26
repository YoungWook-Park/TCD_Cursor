using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Simulator;
using Xunit;

namespace Tcd.Simulator.Tests
{
  public class SimMotionServiceTests
  {
    private static SimMotionService CreateService()
    {
      var sim = new TcdSimulation();
      var settings = new AppSettingsProxy(TimeSpan.FromSeconds(3));
      return new SimMotionService(sim, settings);
    }

    [Fact]
    public void Constructor_NullSimulation_Throws()
    {
      var settings = new AppSettingsProxy(TimeSpan.FromSeconds(3));
      Assert.Throws<ArgumentNullException>(() =>
        new SimMotionService(null, settings));
    }

    [Fact]
    public void Constructor_NullSettings_Throws()
    {
      var sim = new TcdSimulation();
      Assert.Throws<ArgumentNullException>(() =>
        new SimMotionService(sim, null));
    }

    [Fact]
    public async Task ServoOnAsync_SetsAxisIsServoOnTrue()
    {
      var svc = CreateService();
      await svc.ServoOnAsync("U", CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.True(state.IsServoOn);
    }

    [Fact]
    public async Task ServoOffAsync_SetsAxisIsServoOnFalse()
    {
      var svc = CreateService();
      await svc.ServoOnAsync("U", CancellationToken.None);
      await svc.ServoOffAsync("U", CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.False(state.IsServoOn);
    }

    [Fact]
    public async Task FaultClearAsync_CompletesWithoutException()
    {
      var svc = CreateService();
      await svc.FaultClearAsync("U", CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutException()
    {
      var svc = CreateService();
      await svc.StopAsync("U", CancellationToken.None);
    }

    [Fact]
    public async Task GetAxisState_ReflectsCurrentSimAxisPosition()
    {
      var svc = CreateService();
      await svc.ServoOnAsync("U", CancellationToken.None);
      await svc.AbsMoveAsync("U", 1.0, CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.Equal(1.0, state.Position, 2);
    }

    [Fact]
    public async Task GetAxisState_ReflectsIsServoOn()
    {
      var svc = CreateService();
      await svc.ServoOnAsync("U", CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.True(state.IsServoOn);
    }

    [Fact]
    public async Task GetAxisState_IsHome_TrueWhenPositionNearZero()
    {
      var svc = CreateService();
      await svc.AbsMoveAsync("U", 0.0, CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.True(state.IsHome);
    }

    [Fact]
    public void GetSnapshot_ReturnsExactlyFiveAxes()
    {
      var svc = CreateService();
      var snapshot = svc.GetSnapshot();
      Assert.Equal(5, snapshot.Count);
    }

    [Fact]
    public void GetSnapshot_AxisOrder_IsUVWZLowerZUpper()
    {
      var svc = CreateService();
      var snapshot = svc.GetSnapshot();
      Assert.Equal("U", snapshot[0].AxisName);
      Assert.Equal("V", snapshot[1].AxisName);
      Assert.Equal("W", snapshot[2].AxisName);
      Assert.Equal("ZLower", snapshot[3].AxisName);
      Assert.Equal("ZUpper", snapshot[4].AxisName);
    }

    [Fact]
    public async Task HomeAsync_MovesAxisToZeroPosition()
    {
      var svc = CreateService();
      await svc.AbsMoveAsync("V", 1.0, CancellationToken.None);
      await svc.HomeAsync("V", CancellationToken.None);
      var state = svc.GetAxisState("V");
      Assert.Equal(0.0, state.Position, 2);
    }

    [Fact]
    public async Task AbsMoveAsync_MovesAxisToTargetPosition()
    {
      var svc = CreateService();
      await svc.AbsMoveAsync("W", 1.0, CancellationToken.None);
      var state = svc.GetAxisState("W");
      Assert.Equal(1.0, state.Position, 2);
    }

    [Fact]
    public async Task IncMoveAsync_IncreasesPositionByDelta()
    {
      var svc = CreateService();
      await svc.AbsMoveAsync("U", 1.0, CancellationToken.None);
      await svc.IncMoveAsync("U", 1.0, CancellationToken.None);
      var state = svc.GetAxisState("U");
      Assert.Equal(2.0, state.Position, 2);
    }

    [Fact]
    public async Task JogAsync_PositiveVelocity_PositionIncreases()
    {
      var svc = CreateService();
      var before = svc.GetAxisState("U").Position;
      await svc.JogAsync("U", 1.0, CancellationToken.None);
      var after = svc.GetAxisState("U").Position;
      Assert.True(after > before);
    }

    [Fact]
    public async Task UnknownAxisName_ThrowsArgumentOutOfRangeException()
    {
      var svc = CreateService();
      await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
        () => svc.AbsMoveAsync("INVALID", 0, CancellationToken.None));
    }

    [Fact]
    public async Task ZLower_And_ZUpper_BothMapToSameSimAxis()
    {
      var svc = CreateService();
      await svc.AbsMoveAsync("ZLower", 5.0, CancellationToken.None);
      var upper = svc.GetAxisState("ZUpper");
      Assert.Equal(5.0, upper.Position, 2);
    }
  }
}
