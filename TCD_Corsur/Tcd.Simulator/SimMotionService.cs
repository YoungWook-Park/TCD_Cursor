using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;

namespace Tcd.Simulator
{
    /// <summary>
    /// IMotionService + IAxisStateProvider implementation backed by SimLowerChamberMotion.
    /// State is read from sim axes (no background task; sim state is in-process).
    /// </summary>
    public sealed class SimMotionService : IMotionService, IAxisStateProvider
    {
        private static readonly string[] SimAxisOrder = { "U", "V", "W", "ZLower", "ZUpper" };

        private readonly TcdSimulation _sim;
        private readonly AppSettingsProxy _settings;

        public SimMotionService(TcdSimulation simulation, AppSettingsProxy settings)
        {
            _sim = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public AxisState GetAxisState(string axisName)
        {
            var a = Axis(axisName);
            double pos = a.Position;
            return new AxisState
            {
                AxisName = axisName,
                Position = pos,
                IsMoving = false,
                IsFault = false,
                IsHome = Math.Abs(pos) < 0.01
            };
        }

        public IReadOnlyList<AxisState> GetSnapshot()
        {
            var list = new List<AxisState>(5);
            foreach (var name in SimAxisOrder)
            {
                list.Add(GetAxisState(name));
            }
            return list;
        }

        public Task AbsMoveAsync(string axis, double targetPosition, CancellationToken cancellationToken)
        {
            var a = Axis(axis);
            return MoveAndWaitAsync(a, targetPosition, cancellationToken);
        }

        public Task IncMoveAsync(string axis, double delta, CancellationToken cancellationToken)
        {
            var a = Axis(axis);
            var target = a.Position + delta;
            return MoveAndWaitAsync(a, target, cancellationToken);
        }

        public Task JogAsync(string axis, double velocity, CancellationToken cancellationToken)
        {
            // Simple simulator: treat jog as small incremental move in the given direction.
            var delta = Math.Sign(velocity) * 10.0;
            return IncMoveAsync(axis, delta, cancellationToken);
        }

        public Task StopAsync(string axis, CancellationToken cancellationToken)
        {
            // Simulator: nothing special to do; real SPII would stop the buffer/axis.
            return Task.CompletedTask;
        }

        public Task HomeAsync(string axis, CancellationToken cancellationToken)
        {
            // Simulator: treat home as move to 0.
            return AbsMoveAsync(axis, 0, cancellationToken);
        }

        public Task FaultClearAsync(string axis, CancellationToken cancellationToken)
        {
            // Simulator: nothing to clear.
            return Task.CompletedTask;
        }

        public Task ServoOnAsync(string axis, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ServoOffAsync(string axis, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task MoveAndWaitAsync(IAxis axis, double target, CancellationToken cancellationToken)
        {
            await axis.CommandMoveToAsync(target, cancellationToken).ConfigureAwait(false);
            await axis.WaitForInPositionAsync(target, 0.01, _settings.AxisMoveTimeout, cancellationToken).ConfigureAwait(false);
        }

        private IAxis Axis(string axis)
        {
            axis = (axis ?? string.Empty).Trim().ToUpperInvariant();
            if (axis == "U") return _sim.LowerMotion.U;
            if (axis == "V") return _sim.LowerMotion.V;
            if (axis == "W") return _sim.LowerMotion.W;
            if (axis == "Z" || axis == "ZLOWER" || axis == "ZUPPER") return _sim.LowerMotion.Z;
            throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown axis");
        }
    }

    /// <summary>
    /// Small adapter so SimMotionService can read timeout settings without referencing App project types.
    /// </summary>
    public sealed class AppSettingsProxy
    {
        public TimeSpan AxisMoveTimeout { get; }

        public AppSettingsProxy(TimeSpan axisMoveTimeout)
        {
            AxisMoveTimeout = axisMoveTimeout;
        }
    }
}

