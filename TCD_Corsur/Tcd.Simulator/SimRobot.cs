using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;

namespace Tcd.Simulator
{
    public sealed class SimRobot : IRobot
    {
        private readonly object _gate = new object();
        private readonly ITimeProvider _time;
        private readonly IMaterialTracker _materials;
        private RobotPosition _pos;
        private Material _holding;

        public SimRobot(ITimeProvider time, IMaterialTracker materials)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _pos = RobotPosition.Home;
        }

        public RobotPosition CurrentPosition { get { lock (_gate) return _pos; } }
        public bool HasVacuum { get { lock (_gate) return _holding != null; } }

        public Task CommandMoveToAsync(RobotPosition position, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _ = RunMoveAsync(position, cancellationToken);
                return Task.CompletedTask;
            }
        }

        public async Task WaitForPositionAsync(RobotPosition position, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var start = _time.Now;
            while (_time.Now - start < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CurrentPosition == position) return;
                await _time.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"Robot timeout waiting for {position} (cur={CurrentPosition}).");
        }

        private async Task RunMoveAsync(RobotPosition position, CancellationToken cancellationToken)
        {
            await _time.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            lock (_gate) _pos = position;
        }

        public Task PickAsync(MaterialLocation from, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                if (_holding != null) throw new InvalidOperationException("Robot already holding material.");
            }

            var m = _materials.Remove(from);
            if (m == null) throw new InvalidOperationException($"No material at {from}");

            lock (_gate) _holding = m.With(state: MaterialState.InProcess, location: MaterialLocation.Robot);
            return Task.CompletedTask;
        }

        public Task PlaceAsync(MaterialLocation to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Material m;
            lock (_gate)
            {
                if (_holding == null) throw new InvalidOperationException("Robot is not holding material.");
                m = _holding;
                _holding = null;
            }

            _materials.Place(m.With(location: to), to);
            return Task.CompletedTask;
        }
    }
}

