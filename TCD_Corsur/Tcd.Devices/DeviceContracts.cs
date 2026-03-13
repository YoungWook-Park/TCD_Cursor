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

    public interface IAxis
    {
        string Name { get; }
        double Position { get; }
        Task CommandMoveToAsync(double position, CancellationToken cancellationToken);
        Task WaitForInPositionAsync(double position, double tolerance, TimeSpan timeout, CancellationToken cancellationToken);
    }

    public interface ILowerChamberMotion
    {
        IAxis U { get; }
        IAxis V { get; }
        IAxis W { get; }
        IAxis Z { get; } // Up/Down (bonding lift)

        Task CommandMoveToBondingPositionAsync(CancellationToken cancellationToken);
        Task WaitForBondingPositionAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task CommandMoveToLoadPositionAsync(CancellationToken cancellationToken);
        Task WaitForLoadPositionAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }

    public interface IChamber
    {
        MaterialLocation Location { get; }
        bool IsOccupied { get; }
    }

    public interface IPlc
    {
        Task<bool> WaitForStageLoadedAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}

