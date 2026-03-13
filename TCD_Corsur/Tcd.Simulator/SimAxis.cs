using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;

namespace Tcd.Simulator
{
    public sealed class SimAxis : IAxis
    {
        private readonly object _gate = new object();
        private readonly ITimeProvider _time;
        private readonly double _unitsPerSecond;
        private double _position;

        public SimAxis(string name, ITimeProvider time, double unitsPerSecond = 50)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _unitsPerSecond = unitsPerSecond <= 0 ? 50 : unitsPerSecond;
        }

        public string Name { get; }

        public double Position
        {
            get { lock (_gate) return _position; }
            private set { lock (_gate) _position = value; }
        }

        public Task CommandMoveToAsync(double position, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _ = RunMoveAsync(position, cancellationToken);
                return Task.CompletedTask;
            }
        }

        public async Task WaitForInPositionAsync(double position, double tolerance, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var start = _time.Now;
            while (_time.Now - start < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pos = Position;
                if (Math.Abs(pos - position) <= tolerance)
                {
                    return;
                }
                await _time.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"{Name} axis timeout waiting for {position:0.###}±{tolerance:0.###} (cur={Position:0.###}).");
        }

        private async Task RunMoveAsync(double position, CancellationToken cancellationToken)
        {
            var from = Position;
            var distance = Math.Abs(position - from);
            var seconds = distance / _unitsPerSecond;
            await _time.Delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);
            Position = position;
        }
    }
}

