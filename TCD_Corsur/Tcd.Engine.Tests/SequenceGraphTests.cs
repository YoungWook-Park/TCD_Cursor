using System;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Engine.Tests
{
  public sealed class SequenceGraphTests
  {
    [Fact]
    public void Constructor_NullStartNodeId_Throws()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new SequenceGraph(null));
    }

    [Fact]
    public void AddNode_NullNode_Throws()
    {
      var graph = new SequenceGraph("start");

      Assert.Throws<ArgumentNullException>(() =>
        graph.AddNode(null));
    }

    [Fact]
    public void AddNode_DuplicateId_Throws()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));

      Assert.Throws<ArgumentException>(() =>
        graph.AddNode(new ActionNode("n1", "N1dup",
          (ctx, ct) => Task.CompletedTask)));
    }

    [Fact]
    public void Contains_AfterAddNode_ReturnsTrue()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));

      Assert.True(graph.Contains("n1"));
    }

    [Fact]
    public void Contains_UnknownId_ReturnsFalse()
    {
      var graph = new SequenceGraph("n1");

      Assert.False(graph.Contains("unknown"));
    }

    [Fact]
    public void GetNode_KnownId_ReturnsNode()
    {
      var graph = new SequenceGraph("n1");
      var node = new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask);
      graph.AddNode(node);

      var retrieved = graph.GetNode("n1");

      Assert.Same(node, retrieved);
    }

    [Fact]
    public void GetNode_UnknownId_ThrowsKeyNotFoundException()
    {
      var graph = new SequenceGraph("n1");

      Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
        graph.GetNode("unknown"));
    }

    [Fact]
    public void TryGetNext_AfterSetNext_ReturnsTrueAndNodeId()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => Task.CompletedTask));
      graph.SetNext("n1", "n2");

      var found = graph.TryGetNext("n1", out var nextId);

      Assert.True(found);
      Assert.Equal("n2", nextId);
    }

    [Fact]
    public void TryGetNext_NoEdge_ReturnsFalse()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));

      var found = graph.TryGetNext("n1", out _);

      Assert.False(found);
    }

    [Fact]
    public void SetNext_OverwritesExistingEdge()
    {
      var graph = new SequenceGraph("n1");
      graph.AddNode(new ActionNode("n1", "N1",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("n2", "N2",
        (ctx, ct) => Task.CompletedTask));
      graph.AddNode(new ActionNode("n3", "N3",
        (ctx, ct) => Task.CompletedTask));
      graph.SetNext("n1", "n2");
      graph.SetNext("n1", "n3");

      graph.TryGetNext("n1", out var nextId);

      Assert.Equal("n3", nextId);
    }
  }
}
