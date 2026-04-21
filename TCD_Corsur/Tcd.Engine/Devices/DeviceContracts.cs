using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Materials;

namespace Tcd.Devices
{
    public enum RobotPosition
    {
        Home = 0,
        Stage = 1,
        UpperChamberLoad = 2,
        LowerChamberLoad = 3,
    }

    public interface IRobot
    {
        RobotPosition CurrentPosition { get; }
        bool HasVacuum { get; }

        Task CommandMoveToAsync(RobotPosition position, CancellationToken cancellationToken);
        Task WaitForPositionAsync(RobotPosition position, TimeSpan timeout, CancellationToken cancellationToken);

        Task PickAsync(MaterialLocation from, CancellationToken cancellationToken);
        Task PlaceAsync(MaterialLocation to, CancellationToken cancellationToken);
    }

    public interface IPlc
    {
        Task<bool> WaitForStageLoadedAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
