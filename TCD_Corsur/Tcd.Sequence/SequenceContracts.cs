using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Sequence
{
    public enum SequenceStatus
    {
        Succeeded = 0,
        Failed = 1,
        Stopped = 2,
    }

    public sealed class SequenceResult
    {
        private SequenceResult(SequenceStatus status, string error)
        {
            Status = status;
            Error = error;
        }

        public SequenceStatus Status { get; }
        public string Error { get; }

        public static SequenceResult Success() => new SequenceResult(SequenceStatus.Succeeded, null);
        public static SequenceResult Fail(string error) => new SequenceResult(SequenceStatus.Failed, error ?? "Unknown error");
        public static SequenceResult Stopped() => new SequenceResult(SequenceStatus.Stopped, "Stopped");
    }

    public interface ISequence
    {
        string Key { get; }
        string DisplayName { get; }
        Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken);
    }

    public sealed class DelegateSequence : ISequence
    {
        private readonly Func<ISequenceContext, object, CancellationToken, Task> _body;

        public DelegateSequence(string key, string displayName, Func<ISequenceContext, object, CancellationToken, Task> body)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? key;
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public string Key { get; }
        public string DisplayName { get; }

        public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
        {
            try
            {
                await _body(context, parameter, cancellationToken).ConfigureAwait(false);
                return SequenceResult.Success();
            }
            catch (OperationCanceledException)
            {
                return SequenceResult.Stopped();
            }
            catch (Exception ex)
            {
                context.Alarms.Raise(new Core.Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", Core.AlarmSeverity.Error, context.Time.Now));
                return SequenceResult.Fail(ex.Message);
            }
        }
    }
}

