using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Core
{
    public interface ITimeProvider
    {
        DateTimeOffset Now { get; }
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
            return Task.Delay(delay, cancellationToken);
        }
    }
}

