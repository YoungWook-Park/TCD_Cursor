using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Devices
{
    /// <summary>
    /// Abstract motion service for axes (SPIIPLUS or simulator behind this interface).
    /// Axis identifier is a logical name, e.g. "U","V","W","ZLower","ZUpper".
    /// </summary>
    public interface IMotionService
    {
        Task AbsMoveAsync(string axis, double targetPosition, CancellationToken cancellationToken);
        Task IncMoveAsync(string axis, double delta, CancellationToken cancellationToken);
        Task JogAsync(string axis, double velocity, CancellationToken cancellationToken);
        Task StopAsync(string axis, CancellationToken cancellationToken);
        Task HomeAsync(string axis, CancellationToken cancellationToken);
        Task FaultClearAsync(string axis, CancellationToken cancellationToken);
        Task ServoOnAsync(string axis, CancellationToken cancellationToken);
        Task ServoOffAsync(string axis, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Snapshot of a single axis state (cached by monitoring task; do not read from device in UI).
    /// </summary>
    public sealed class AxisState
    {
        public string AxisName { get; set; } = "";
        public double Position { get; set; }
        public bool IsMoving { get; set; }
        public bool IsFault { get; set; }
        public bool IsHome { get; set; }
        public bool IsServoOn { get; set; }
        public bool IsLimitPos { get; set; }
        public bool IsLimitNeg { get; set; }
    }

    /// <summary>
    /// Read-only motion state. Implementations update internal cache from a background monitor;
    /// callers (e.g. ViewModel) only read cached values. Do not call device/ACS from UI.
    /// </summary>
    public interface IAxisStateProvider
    {
        AxisState GetAxisState(string axisName);
        /// <summary>Returns all axes in order: U, V, W, ZLower, ZUpper (index 0..4).</summary>
        IReadOnlyList<AxisState> GetSnapshot();
    }
}
