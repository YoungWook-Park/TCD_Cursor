using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Core.Logging
{
    /// <summary>
    /// Single instance recommended. Callers enqueue only; background consumer batches and writes via ILogSink.
    /// </summary>
    public sealed class LogWriter : ILogWriter
    {
        private readonly ILogSink _sink;
        private readonly BlockingCollection<LogEntry> _queue;
        private readonly int _batchSize;
        private readonly int _batchWaitMs;
        private readonly LogLevel _minLevel;
        private Task _consumerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public LogWriter(ILogSink sink, int boundedCapacity = 4096, int batchSize = 100, int batchWaitMs = 500)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _batchSize = batchSize > 0 ? batchSize : 100;
            _batchWaitMs = batchWaitMs > 0 ? batchWaitMs : 500;
            _minLevel = sink.MinLevel;
            _queue = new BlockingCollection<LogEntry>(boundedCapacity);
        }

        public void Start()
        {
            if (_consumerTask != null) return;
            _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        public void Stop()
        {
            try
            {
                _cts.Cancel();
                _queue.CompleteAdding();
                _consumerTask?.GetAwaiter().GetResult();
            }
            catch { }
        }

        public void Log(LogLevel level, in LogContext ctx, string stepName, string message,
            IReadOnlyDictionary<string, object> data = null, Exception ex = null)
        {
            if (level < _minLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = level,
                SequenceKey = ctx.SequenceKey ?? "",
                RunId = ctx.RunId,
                StepName = stepName ?? "",
                AxisName = ctx.AxisName ?? "",
                Message = message ?? "",
                Data = data,
                ExceptionMessage = ex?.Message ?? ""
            };

            _queue.TryAdd(entry);
        }

        public void Trace(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null)
            => Log(LogLevel.Trace, ctx, stepName, message, data, null);

        public void Debug(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null)
            => Log(LogLevel.Debug, ctx, stepName, message, data, null);

        public void Info(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null)
            => Log(LogLevel.Info, ctx, stepName, message, data, null);

        public void Warn(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null)
            => Log(LogLevel.Warn, ctx, stepName, message, data, null);

        public void Error(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null, Exception ex = null)
            => Log(LogLevel.Error, ctx, stepName, message, data, ex);

        public void Fatal(in LogContext ctx, string stepName, string message, IReadOnlyDictionary<string, object> data = null, Exception ex = null)
            => Log(LogLevel.Fatal, ctx, stepName, message, data, ex);

        private async Task ConsumeAsync(CancellationToken ct)
        {
            var batch = new List<LogEntry>(_batchSize);
            var waitMs = Math.Min(100, _batchWaitMs);

            while (!ct.IsCancellationRequested)
            {
                batch.Clear();
                var deadline = DateTime.UtcNow.AddMilliseconds(_batchWaitMs);

                while (batch.Count < _batchSize && DateTime.UtcNow < deadline)
                {
                    if (ct.IsCancellationRequested) break;
                    if (_queue.TryTake(out var entry, waitMs))
                        batch.Add(entry);
                }

                if (batch.Count > 0)
                {
                    try
                    {
                        await _sink.WriteBatchAsync(batch, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                }

                if (_queue.IsCompleted) break;
            }
        }
    }
}
