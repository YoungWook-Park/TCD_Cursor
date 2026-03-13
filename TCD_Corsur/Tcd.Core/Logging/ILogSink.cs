using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Core.Logging
{
    public interface ILogSink
    {
        LogLevel MinLevel { get; }
        Task WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken);
    }
}
