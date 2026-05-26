using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Simulator;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Simulator.Tests
{
  public class SimLowerChamberMotionTests
  {
    [Fact]
    public void Constructor_NullTime_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimLowerChamberMotion(null));
    }

    [Fact]
    public void FourAxes_AllInitializedWithCorrectNames()
    {
      var motion = new SimLowerChamberMotion(new FakeTimeProvider());

      Assert.Equal("U", motion.U.Name);
      Assert.Equal("V", motion.V.Name);
      Assert.Equal("W", motion.W.Name);
      Assert.Equal("Z", motion.Z.Name);
    }

    [Fact]
    public async Task CommandMoveToBondingPosition_WithFakeTime_AllAxesReachTarget()
    {
      var fakeTime = new FakeTimeProvider();
      var motion = new SimLowerChamberMotion(fakeTime, bondZ: 100, loadZ: 0);

      await motion.CommandMoveToBondingPositionAsync(CancellationToken.None);
      await motion.WaitForBondingPositionAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);

      Assert.Equal(0.0, motion.U.Position, 2);
      Assert.Equal(0.0, motion.V.Position, 2);
      Assert.Equal(0.0, motion.W.Position, 2);
      Assert.Equal(100.0, motion.Z.Position, 2);
    }

    [Fact]
    public async Task WaitForBondingPosition_WithFakeTime_Succeeds()
    {
      var fakeTime = new FakeTimeProvider();
      var motion = new SimLowerChamberMotion(fakeTime, bondZ: 0, loadZ: 0);

      await motion.WaitForBondingPositionAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);
    }

    [Fact]
    public async Task CommandMoveToLoadPosition_WithFakeTime_ZReachesLoadZ()
    {
      var fakeTime = new FakeTimeProvider();
      var motion = new SimLowerChamberMotion(fakeTime, bondZ: 100, loadZ: 0);

      await motion.CommandMoveToBondingPositionAsync(CancellationToken.None);
      await motion.WaitForBondingPositionAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);

      await motion.CommandMoveToLoadPositionAsync(CancellationToken.None);
      await motion.WaitForLoadPositionAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);

      Assert.Equal(0.0, motion.Z.Position, 2);
    }

    [Fact]
    public async Task WaitForLoadPosition_WithFakeTime_Succeeds()
    {
      var fakeTime = new FakeTimeProvider();
      var motion = new SimLowerChamberMotion(fakeTime, bondZ: 100, loadZ: 0);

      await motion.WaitForLoadPositionAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);
    }
  }
}
