using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Sequence
{
    public sealed class SequenceManager
    {
        private readonly Dictionary<string, ISequence> _sequences =
            new Dictionary<string, ISequence>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SequenceTraceEventArgs> Trace;

        public void Register(ISequence sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            _sequences[sequence.Key] = sequence;
        }

        public bool Contains(string key) => _sequences.ContainsKey(key);

        public IReadOnlyCollection<ISequence> List()
        {
            return new List<ISequence>(_sequences.Values).AsReadOnly();
        }

        public async Task<SequenceResult> RunAsync(string key, ISequenceContext context, object parameter, CancellationToken cancellationToken)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (context == null) throw new ArgumentNullException(nameof(context));

            ISequence seq;
            if (!_sequences.TryGetValue(key, out seq))
                throw new KeyNotFoundException($"Sequence not found: {key}");

            Trace?.Invoke(this, new SequenceTraceEventArgs(key, seq.DisplayName, SequenceTraceKind.Started));
            var result = await seq.ExecuteAsync(context, parameter, cancellationToken).ConfigureAwait(false);
            Trace?.Invoke(this, new SequenceTraceEventArgs(key, seq.DisplayName, SequenceTraceKind.Completed, result.Status, result.Error));
            return result;
        }
    }

    public enum SequenceTraceKind
    {
        Started = 0,
        Completed = 1,
    }

    public sealed class SequenceTraceEventArgs : EventArgs
    {
        public SequenceTraceEventArgs(string key, string displayName, SequenceTraceKind kind, SequenceStatus? status = null, string error = null)
        {
            Key = key;
            DisplayName = displayName;
            Kind = kind;
            Status = status;
            Error = error;
            Timestamp = DateTimeOffset.Now;
        }

        public DateTimeOffset Timestamp { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public SequenceTraceKind Kind { get; }
        public SequenceStatus? Status { get; }
        public string Error { get; }
    }
}
