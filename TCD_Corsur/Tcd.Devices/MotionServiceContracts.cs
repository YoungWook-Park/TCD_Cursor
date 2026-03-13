using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Devices
{
    /// <summary>
    /// Abstract motion service for axes (SPIIPLUS or simulator behind this interface).
    /// Axis identifier is a logical name, e.g. "U","V","W","Z".
    /// </summary>
    public interface IMotionService
    {
        Task AbsMoveAsync(string axis, double targetPosition, CancellationToken cancellationToken);
        Task IncMoveAsync(string axis, double delta, CancellationToken cancellationToken);
        Task JogAsync(string axis, double velocity, CancellationToken cancellationToken);
        Task StopAsync(string axis, CancellationToken cancellationToken);
        Task HomeAsync(string axis, CancellationToken cancellationToken);
        Task FaultClearAsync(string axis, CancellationToken cancellationToken);
    }
}

