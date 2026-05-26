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
  public class SimPlcTests
  {
    [Fact]
    public void Constructor_NullTime_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimPlc(null, new InMemoryMaterialTracker()));
    }

    [Fact]
    public void Constructor_NullMaterials_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SimPlc(new FakeTimeProvider(), null));
    }

    [Fact]
    public async Task ReadBitAsync_InitiallyFalse()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      var result = await plc.ReadBitAsync(DiBit.EStop_OK, CancellationToken.None);

      Assert.False(result);
    }

    [Fact]
    public async Task WriteBitAsync_SetTrue_ReadBitReturnsTrue()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      await plc.WriteBitAsync(DoBit.LowStageVacOn, true, CancellationToken.None);
      var result = await plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowStageVacOn, CancellationToken.None);

      Assert.True(result);
    }

    [Fact]
    public async Task WriteBitAsync_SetFalse_AfterTrue_ReadBitReturnsFalse()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      await plc.WriteBitAsync(DoBit.LowStageVacOn, true, CancellationToken.None);
      await plc.WriteBitAsync(DoBit.LowStageVacOn, false, CancellationToken.None);
      var result = await plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowStageVacOn, CancellationToken.None);

      Assert.False(result);
    }

    [Fact]
    public async Task ReadWordAsync_InitiallyZero()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      var result = await plc.ReadWordAsync(AiWord.ChamberPressure, CancellationToken.None);

      Assert.Equal((short)0, result);
    }

    [Fact]
    public async Task WriteWordAsync_Value_ReadWordReturnsValue()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      await plc.WriteWordAsync(AoWord.EscVoltage, (short)1234, CancellationToken.None);
      var result = await plc.ReadWordAsync(
        (AiWord)(int)AoWord.EscVoltage, CancellationToken.None);

      Assert.Equal((short)1234, result);
    }

    [Fact]
    public async Task ReadBitAsync_BitIndex_MapsToCorrectByteAndBit()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      await plc.WriteBitAsync(
        (DoBit)(int)DiBit.EStop_OK, true, CancellationToken.None);
      var eStopOk = await plc.ReadBitAsync(DiBit.EStop_OK, CancellationToken.None);
      var doorClosed = await plc.ReadBitAsync(DiBit.DoorClosed, CancellationToken.None);

      Assert.True(eStopOk);
      Assert.False(doorClosed);
    }

    [Fact]
    public async Task WaitForStageLoadedAsync_BothStagesOccupied_ReturnsTrue()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var plc = new SimPlc(time, materials);
      var mat1 = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      var mat2 = new Material(
        Guid.NewGuid(), MaterialKind.LowerFilm,
        MaterialState.Loaded, MaterialLocation.Stage2);
      materials.Place(mat1, MaterialLocation.Stage1);
      materials.Place(mat2, MaterialLocation.Stage2);

      var result = await plc.WaitForStageLoadedAsync(
        TimeSpan.FromSeconds(5), CancellationToken.None);

      Assert.True(result);
    }

    [Fact]
    public async Task WaitForStageLoadedAsync_NoMaterials_TimesOut_ReturnsFalse()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var plc = new SimPlc(time, materials);

      var result = await plc.WaitForStageLoadedAsync(
        TimeSpan.Zero, CancellationToken.None);

      Assert.False(result);
    }

    [Fact]
    public async Task WaitForStageLoadedAsync_OnlyStage1_ReturnsFalse()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var plc = new SimPlc(time, materials);
      var mat = new Material(
        Guid.NewGuid(), MaterialKind.UpperFilm,
        MaterialState.Loaded, MaterialLocation.Stage1);
      materials.Place(mat, MaterialLocation.Stage1);

      var result = await plc.WaitForStageLoadedAsync(
        TimeSpan.Zero, CancellationToken.None);

      Assert.False(result);
    }

    [Fact]
    public async Task WaitForStageLoadedAsync_Cancelled_ThrowsOperationCanceledException()
    {
      var time = new FakeTimeProvider();
      var materials = new InMemoryMaterialTracker();
      var plc = new SimPlc(time, materials);
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        plc.WaitForStageLoadedAsync(TimeSpan.FromSeconds(10), cts.Token));
    }

    [Fact]
    public void StartAndStopMonitoring_NoOp_DoesNotThrow()
    {
      var plc = new SimPlc(new FakeTimeProvider(), new InMemoryMaterialTracker());

      var ex = Record.Exception(() =>
      {
        plc.StartMonitoring(TimeSpan.FromSeconds(1));
        plc.StopMonitoring();
      });

      Assert.Null(ex);
    }
  }
}
