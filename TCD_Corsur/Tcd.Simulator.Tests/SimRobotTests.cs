using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Simulator;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Simulator.Tests
{
  public class SimRobotTests
  {
    [Fact]
    public void Constructor_NullTimeProvider_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimRobot(null, new InMemoryMaterialTracker()));
    }

    [Fact]
    public void Constructor_NullMaterials_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimRobot(new FakeTimeProvider(), null));
    }

    [Fact]
    public void InitialPosition_IsHome()
    {
      var robot = new SimRobot(new FakeTimeProvider(), new InMemoryMaterialTracker());
      Assert.Equal(RobotPosition.Home, robot.CurrentPosition);
    }

    [Fact]
    public void HasVacuum_Initial_IsFalse()
    {
      var robot = new SimRobot(new FakeTimeProvider(), new InMemoryMaterialTracker());
      Assert.False(robot.HasVacuum);
    }

    [Fact]
    public async Task CommandMoveToAsync_WithFakeTime_UpdatesCurrentPosition()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);

      await robot.CommandMoveToAsync(RobotPosition.Stage, CancellationToken.None);
      await robot.WaitForPositionAsync(
        RobotPosition.Stage, TimeSpan.FromSeconds(1), CancellationToken.None);

      Assert.Equal(RobotPosition.Stage, robot.CurrentPosition);
    }

    [Fact]
    public async Task WaitForPositionAsync_AlreadyAtTarget_ReturnsImmediately()
    {
      var time = new FakeTimeProvider();
      var robot = new SimRobot(time, new InMemoryMaterialTracker());

      await robot.WaitForPositionAsync(
        RobotPosition.Home, TimeSpan.FromSeconds(1), CancellationToken.None);
    }

    [Fact]
    public async Task WaitForPositionAsync_AfterCommandMove_WithFakeTime_Succeeds()
    {
      var time = new FakeTimeProvider();
      var robot = new SimRobot(time, new InMemoryMaterialTracker());

      await robot.CommandMoveToAsync(RobotPosition.UpperChamberLoad, CancellationToken.None);
      await robot.WaitForPositionAsync(
        RobotPosition.UpperChamberLoad, TimeSpan.FromSeconds(1), CancellationToken.None);

      Assert.Equal(RobotPosition.UpperChamberLoad, robot.CurrentPosition);
    }

    [Fact]
    public async Task WaitForPositionAsync_TimesOut_ThrowsTimeoutException()
    {
      var robot = new SimRobot(new SystemTimeProvider(), new InMemoryMaterialTracker());

      await Assert.ThrowsAsync<TimeoutException>(() =>
        robot.WaitForPositionAsync(
          RobotPosition.Stage,
          TimeSpan.FromMilliseconds(10),
          CancellationToken.None));
    }

    [Fact]
    public async Task WaitForPositionAsync_Cancellation_ThrowsOperationCanceledException()
    {
      var robot = new SimRobot(new SystemTimeProvider(), new InMemoryMaterialTracker());
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        robot.WaitForPositionAsync(
          RobotPosition.Stage,
          TimeSpan.FromSeconds(5),
          cts.Token));
    }

    [Fact]
    public async Task PickAsync_MaterialAtLocation_SetsHasVacuumTrue()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);

      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      Assert.True(robot.HasVacuum);
    }

    [Fact]
    public async Task PickAsync_RemovesMaterialFromTracker()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);

      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      Assert.Null(materials.Get(MaterialLocation.Stage1));
    }

    [Fact]
    public async Task PickAsync_HeldMaterial_LocationIsRobot()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);

      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      Assert.True(robot.HasVacuum);
    }

    [Fact]
    public async Task PickAsync_AlreadyHolding_ThrowsInvalidOperationException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat1 = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      var mat2 = new Material(
        Guid.NewGuid(), MaterialKind.LowerFilm,
        MaterialState.Loaded, MaterialLocation.Stage2);
      materials.Place(mat1, MaterialLocation.Stage1);
      materials.Place(mat2, MaterialLocation.Stage2);

      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        robot.PickAsync(MaterialLocation.Stage2, CancellationToken.None));
    }

    [Fact]
    public async Task PickAsync_NoMaterialAtLocation_ThrowsInvalidOperationException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);

      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None));
    }

    [Fact]
    public async Task PickAsync_Cancelled_ThrowsOperationCanceledException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        robot.PickAsync(MaterialLocation.Stage1, cts.Token));
    }

    [Fact]
    public async Task PlaceAsync_HoldingMaterial_SetsHasVacuumFalse()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);
      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      await robot.PlaceAsync(MaterialLocation.UpperChamber, CancellationToken.None);

      Assert.False(robot.HasVacuum);
    }

    [Fact]
    public async Task PlaceAsync_PutsMaterialIntoTracker()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);
      await robot.PickAsync(MaterialLocation.Stage1, CancellationToken.None);

      await robot.PlaceAsync(MaterialLocation.UpperChamber, CancellationToken.None);

      Assert.NotNull(materials.Get(MaterialLocation.UpperChamber));
    }

    [Fact]
    public async Task PlaceAsync_NotHolding_ThrowsInvalidOperationException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);

      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        robot.PlaceAsync(MaterialLocation.UpperChamber, CancellationToken.None));
    }

    [Fact]
    public async Task PlaceAsync_Cancelled_ThrowsOperationCanceledException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var robot = new SimRobot(time, materials);
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        robot.PlaceAsync(MaterialLocation.UpperChamber, cts.Token));
    }
  }
}
