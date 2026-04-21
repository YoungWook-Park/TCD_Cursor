using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;

namespace Tcd.Sequence
{
    public interface ISequenceContext
    {
        IAlarmSink Alarms { get; }
        ITimeProvider Time { get; }
        CancellationToken StopToken { get; }
    }

    public enum NodeRunStatus
    {
        Succeeded = 0,
        Failed = 1,
        Stopped = 2,
    }

    public sealed class NodeRunResult
    {
        private NodeRunResult(NodeRunStatus status, string error)
        {
            Status = status;
            Error = error;
        }

        public NodeRunStatus Status { get; }
        public string Error { get; }

        public static NodeRunResult Success() => new NodeRunResult(NodeRunStatus.Succeeded, null);
        public static NodeRunResult Fail(string error) => new NodeRunResult(NodeRunStatus.Failed, error ?? "Unknown error");
        public static NodeRunResult Stopped() => new NodeRunResult(NodeRunStatus.Stopped, "Stopped");
    }

    public interface INode
    {
        string Id { get; }
        string DisplayName { get; }
        Task<NodeRunResult> RunAsync(ISequenceContext context, CancellationToken cancellationToken);
    }

    public sealed class ActionNode : INode
    {
        private readonly Func<ISequenceContext, CancellationToken, Task> _action;
        private readonly TimeSpan? _timeout;

        public ActionNode(string id, string displayName, Func<ISequenceContext, CancellationToken, Task> action, TimeSpan? timeout = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _timeout = timeout;
        }

        public string Id { get; }
        public string DisplayName { get; }

        public async Task<NodeRunResult> RunAsync(ISequenceContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (_timeout.HasValue)
                    await Timeouts.WithTimeout(_action(context, cancellationToken), _timeout.Value, cancellationToken).ConfigureAwait(false);
                else
                    await _action(context, cancellationToken).ConfigureAwait(false);
                return NodeRunResult.Success();
            }
            catch (OperationCanceledException) { return NodeRunResult.Stopped(); }
            catch (TimeoutException te)
            {
                context.Alarms.Raise(new Alarm("SEQ_TIMEOUT", $"{DisplayName}: {te.Message}", AlarmSeverity.Error, context.Time.Now));
                return NodeRunResult.Fail(te.Message);
            }
            catch (Exception ex)
            {
                context.Alarms.Raise(new Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", AlarmSeverity.Error, context.Time.Now));
                return NodeRunResult.Fail(ex.Message);
            }
        }
    }

    public sealed class DecisionNode : INode
    {
        private readonly Func<ISequenceContext, CancellationToken, Task<bool>> _predicate;
        private readonly TimeSpan? _timeout;

        public DecisionNode(string id, string displayName, Func<ISequenceContext, CancellationToken, Task<bool>> predicate, TimeSpan? timeout = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _timeout = timeout;
        }

        public string Id { get; }
        public string DisplayName { get; }

        public async Task<NodeRunResult> RunAsync(ISequenceContext context, CancellationToken cancellationToken)
        {
            try
            {
                bool result;
                if (_timeout.HasValue)
                    result = await Timeouts.WithTimeout(_predicate(context, cancellationToken), _timeout.Value, cancellationToken).ConfigureAwait(false);
                else
                    result = await _predicate(context, cancellationToken).ConfigureAwait(false);

                return result ? NodeRunResult.Success() : NodeRunResult.Fail($"{DisplayName}: predicate returned false");
            }
            catch (OperationCanceledException) { return NodeRunResult.Stopped(); }
            catch (TimeoutException te)
            {
                context.Alarms.Raise(new Alarm("SEQ_TIMEOUT", $"{DisplayName}: {te.Message}", AlarmSeverity.Error, context.Time.Now));
                return NodeRunResult.Fail(te.Message);
            }
            catch (Exception ex)
            {
                context.Alarms.Raise(new Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", AlarmSeverity.Error, context.Time.Now));
                return NodeRunResult.Fail(ex.Message);
            }
        }
    }

    public sealed class ForkNode : INode
    {
        public ForkNode(string id, string displayName, IReadOnlyList<string> branchStartNodeIds, string joinNextNodeId)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            BranchStartNodeIds = branchStartNodeIds ?? throw new ArgumentNullException(nameof(branchStartNodeIds));
            JoinNextNodeId = joinNextNodeId;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> BranchStartNodeIds { get; }
        public string JoinNextNodeId { get; }

        public Task<NodeRunResult> RunAsync(ISequenceContext context, CancellationToken cancellationToken)
            => Task.FromResult(NodeRunResult.Success());
    }

    public sealed class SequenceGraph
    {
        private readonly Dictionary<string, INode> _nodes = new Dictionary<string, INode>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SequenceGraph(string startNodeId)
        {
            StartNodeId = startNodeId ?? throw new ArgumentNullException(nameof(startNodeId));
        }

        public string StartNodeId { get; }

        public SequenceGraph AddNode(INode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _nodes.Add(node.Id, node);
            return this;
        }

        public SequenceGraph SetNext(string fromNodeId, string toNodeId)
        {
            _next[fromNodeId] = toNodeId;
            return this;
        }

        public INode GetNode(string nodeId) => _nodes[nodeId];
        public bool TryGetNext(string fromNodeId, out string toNodeId) => _next.TryGetValue(fromNodeId, out toNodeId);
        public bool Contains(string nodeId) => _nodes.ContainsKey(nodeId);
    }

    public sealed class SequenceRunner
    {
        private readonly SequenceGraph _graph;

        public SequenceRunner(SequenceGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public async Task<NodeRunResult> RunAsync(ISequenceContext context, CancellationToken cancellationToken)
        {
            var nodeId = _graph.StartNodeId;

            while (!string.IsNullOrWhiteSpace(nodeId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = _graph.GetNode(nodeId);

                var fork = node as ForkNode;
                if (fork != null)
                {
                    var branchTasks = new List<Task<NodeRunResult>>(fork.BranchStartNodeIds.Count);
                    foreach (var branchStart in fork.BranchStartNodeIds)
                        branchTasks.Add(RunFromAsync(context, branchStart, cancellationToken));

                    var results = await Task.WhenAll(branchTasks).ConfigureAwait(false);
                    foreach (var r in results)
                        if (r.Status != NodeRunStatus.Succeeded) return r;

                    nodeId = fork.JoinNextNodeId;
                    continue;
                }

                var result = await node.RunAsync(context, cancellationToken).ConfigureAwait(false);
                if (result.Status != NodeRunStatus.Succeeded) return result;

                string next;
                nodeId = _graph.TryGetNext(nodeId, out next) ? next : null;
            }

            return NodeRunResult.Success();
        }

        private async Task<NodeRunResult> RunFromAsync(ISequenceContext context, string startNodeId, CancellationToken cancellationToken)
        {
            var nodeId = startNodeId;
            while (!string.IsNullOrWhiteSpace(nodeId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = _graph.GetNode(nodeId);

                var fork = node as ForkNode;
                if (fork != null)
                {
                    var branchTasks = new List<Task<NodeRunResult>>(fork.BranchStartNodeIds.Count);
                    foreach (var branchStart in fork.BranchStartNodeIds)
                        branchTasks.Add(RunFromAsync(context, branchStart, cancellationToken));

                    var results = await Task.WhenAll(branchTasks).ConfigureAwait(false);
                    foreach (var r in results)
                        if (r.Status != NodeRunStatus.Succeeded) return r;

                    nodeId = fork.JoinNextNodeId;
                    continue;
                }

                var result = await node.RunAsync(context, cancellationToken).ConfigureAwait(false);
                if (result.Status != NodeRunStatus.Succeeded) return result;

                string next;
                nodeId = _graph.TryGetNext(nodeId, out next) ? next : null;
            }

            return NodeRunResult.Success();
        }
    }
}
