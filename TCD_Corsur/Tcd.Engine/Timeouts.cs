using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Core
{
    public static class Timeouts
    {
        public static async Task WithTimeout(Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timeoutTask = Task.Delay(timeout, cts.Token);
                var completed = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (completed == timeoutTask)
                    throw new TimeoutException($"Timed out after {timeout}.");
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
        }

        public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timeoutTask = Task.Delay(timeout, cts.Token);
                var completed = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (completed == timeoutTask)
                    throw new TimeoutException($"Timed out after {timeout}.");
                cts.Cancel();
                return await task.ConfigureAwait(false);
            }
        }
    }
}
