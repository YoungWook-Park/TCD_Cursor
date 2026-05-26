using System;
using System.Collections.Generic;
using Tcd.Materials;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class InMemoryMaterialTrackerTests
  {
    private static Material NewMaterial(
      MaterialLocation location = MaterialLocation.None)
      => new Material(Guid.NewGuid(), MaterialKind.UpperFilm, MaterialState.Loaded, location);

    [Fact]
    public void IsOccupied_EmptyLocation_ReturnsFalse()
    {
      var tracker = new InMemoryMaterialTracker();

      var result = tracker.IsOccupied(MaterialLocation.Stage1);

      Assert.False(result);
    }

    [Fact]
    public void IsOccupied_AfterPlace_ReturnsTrue()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      var result = tracker.IsOccupied(MaterialLocation.Stage1);

      Assert.True(result);
    }

    [Fact]
    public void Place_ValidMaterial_OccupiesLocation()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial();

      tracker.Place(material, MaterialLocation.Stage1);

      Assert.True(tracker.IsOccupied(MaterialLocation.Stage1));
    }

    [Fact]
    public void Place_UpdatesMaterialLocationProperty_ToTarget()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial(MaterialLocation.None);

      tracker.Place(material, MaterialLocation.Stage1);

      var stored = tracker.Get(MaterialLocation.Stage1);
      Assert.Equal(MaterialLocation.Stage1, stored.Location);
    }

    [Fact]
    public void Place_NullMaterial_ThrowsArgumentNullException()
    {
      var tracker = new InMemoryMaterialTracker();

      Assert.Throws<ArgumentNullException>(
        () => tracker.Place(null, MaterialLocation.Stage1));
    }

    [Fact]
    public void Place_NoneLocation_ThrowsArgumentException()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial();

      Assert.Throws<ArgumentException>(
        () => tracker.Place(material, MaterialLocation.None));
    }

    [Fact]
    public void Place_AlreadyOccupied_ThrowsInvalidOperationException()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      Assert.Throws<InvalidOperationException>(
        () => tracker.Place(NewMaterial(), MaterialLocation.Stage1));
    }

    [Fact]
    public void Get_OccupiedLocation_ReturnsMaterial()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial();
      tracker.Place(material, MaterialLocation.Stage1);

      var result = tracker.Get(MaterialLocation.Stage1);

      Assert.NotNull(result);
      Assert.Equal(material.Id, result.Id);
    }

    [Fact]
    public void Get_EmptyLocation_ReturnsNull()
    {
      var tracker = new InMemoryMaterialTracker();

      var result = tracker.Get(MaterialLocation.Stage1);

      Assert.Null(result);
    }

    [Fact]
    public void Remove_OccupiedLocation_ReturnsMaterial()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial();
      tracker.Place(material, MaterialLocation.Stage1);

      var result = tracker.Remove(MaterialLocation.Stage1);

      Assert.NotNull(result);
      Assert.Equal(material.Id, result.Id);
    }

    [Fact]
    public void Remove_OccupiedLocation_SlotBecomesEmpty()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      tracker.Remove(MaterialLocation.Stage1);

      Assert.False(tracker.IsOccupied(MaterialLocation.Stage1));
    }

    [Fact]
    public void Remove_ReturnedMaterial_HasNoneLocation()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      var result = tracker.Remove(MaterialLocation.Stage1);

      Assert.Equal(MaterialLocation.None, result.Location);
    }

    [Fact]
    public void Remove_EmptyLocation_ReturnsNull()
    {
      var tracker = new InMemoryMaterialTracker();

      var result = tracker.Remove(MaterialLocation.Stage1);

      Assert.Null(result);
    }

    [Fact]
    public void Move_TransfersMaterialFromSourceToDestination()
    {
      var tracker = new InMemoryMaterialTracker();
      var material = NewMaterial();
      tracker.Place(material, MaterialLocation.Stage1);

      tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2);

      var atDest = tracker.Get(MaterialLocation.Stage2);
      Assert.NotNull(atDest);
      Assert.Equal(material.Id, atDest.Id);
    }

    [Fact]
    public void Move_SourceBecomesEmpty()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2);

      Assert.False(tracker.IsOccupied(MaterialLocation.Stage1));
    }

    [Fact]
    public void Move_DestinationBecomesOccupied()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2);

      Assert.True(tracker.IsOccupied(MaterialLocation.Stage2));
    }

    [Fact]
    public void Move_MaterialLocation_UpdatedToDestination()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2);

      var atDest = tracker.Get(MaterialLocation.Stage2);
      Assert.Equal(MaterialLocation.Stage2, atDest.Location);
    }

    [Fact]
    public void Move_FromEmpty_ThrowsInvalidOperationException()
    {
      var tracker = new InMemoryMaterialTracker();

      Assert.Throws<InvalidOperationException>(
        () => tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2));
    }

    [Fact]
    public void Move_ToOccupied_ThrowsInvalidOperationException()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);
      tracker.Place(NewMaterial(), MaterialLocation.Stage2);

      Assert.Throws<InvalidOperationException>(
        () => tracker.Move(MaterialLocation.Stage1, MaterialLocation.Stage2));
    }

    [Fact]
    public void Move_NoneFromLocation_ThrowsArgumentException()
    {
      var tracker = new InMemoryMaterialTracker();

      Assert.Throws<ArgumentException>(
        () => tracker.Move(MaterialLocation.None, MaterialLocation.Stage1));
    }

    [Fact]
    public void Move_NoneToLocation_ThrowsArgumentException()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      Assert.Throws<ArgumentException>(
        () => tracker.Move(MaterialLocation.Stage1, MaterialLocation.None));
    }

    [Fact]
    public void Clear_RemovesAllMaterials()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);
      tracker.Place(NewMaterial(), MaterialLocation.Stage2);
      tracker.Place(NewMaterial(), MaterialLocation.Robot);

      tracker.Clear();

      Assert.False(tracker.IsOccupied(MaterialLocation.Stage1));
      Assert.False(tracker.IsOccupied(MaterialLocation.Stage2));
      Assert.False(tracker.IsOccupied(MaterialLocation.Robot));
    }

    [Fact]
    public void Snapshot_ReturnsAllPlacedMaterials()
    {
      var tracker = new InMemoryMaterialTracker();
      var m1 = NewMaterial();
      var m2 = NewMaterial();
      tracker.Place(m1, MaterialLocation.Stage1);
      tracker.Place(m2, MaterialLocation.Stage2);

      var snapshot = tracker.Snapshot();

      Assert.Equal(2, snapshot.Count);
      Assert.True(snapshot.ContainsKey(MaterialLocation.Stage1));
      Assert.True(snapshot.ContainsKey(MaterialLocation.Stage2));
    }

    [Fact]
    public void Snapshot_ReturnsIsolatedCopy_ExternalModifyDoesNotAffect()
    {
      var tracker = new InMemoryMaterialTracker();
      tracker.Place(NewMaterial(), MaterialLocation.Stage1);

      var snapshot = (Dictionary<MaterialLocation, Material>)tracker.Snapshot();
      snapshot.Remove(MaterialLocation.Stage1);

      Assert.True(tracker.IsOccupied(MaterialLocation.Stage1));
    }
  }
}
