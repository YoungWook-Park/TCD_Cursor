using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;

namespace Tcd.Simulator
{
    public sealed class SimLowerChamberMotion : ILowerChamberMotion
    {
        private readonly ITimeProvider _time;

        // Simple, configurable target positions (simulation)
        private readonly double _bondZ;
        private readonly double _loadZ;

        public SimLowerChamberMotion(ITimeProvider time, double bondZ = 100, double loadZ = 0)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _bondZ = bondZ;
            _loadZ = loadZ;

            U = new SimAxis("U", _time, unitsPerSecond: 80);
            V = new SimAxis("V", _time, unitsPerSecond: 80);
            W = new SimAxis("W", _time, unitsPerSecond: 80);
            Z = new SimAxis("Z", _time, unitsPerSecond: 60);
        }

        public IAxis U { get; }
        public IAxis V { get; }
        public IAxis W { get; }
        public IAxis Z { get; }

        public async Task CommandMoveToBondingPositionAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                U.CommandMoveToAsync(0, cancellationToken),
                V.CommandMoveToAsync(0, cancellationToken),
                W.CommandMoveToAsync(0, cancellationToken),
                Z.CommandMoveToAsync(_bondZ, cancellationToken)
            ).ConfigureAwait(false);
        }

        public Task WaitForBondingPositionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            const double tol = 0.01;
            return Task.WhenAll(
                U.WaitForInPositionAsync(0, tol, timeout, cancellationToken),
                V.WaitForInPositionAsync(0, tol, timeout, cancellationToken),
                W.WaitForInPositionAsync(0, tol, timeout, cancellationToken),
                Z.WaitForInPositionAsync(_bondZ, tol, timeout, cancellationToken)
            );
        }

        public Task CommandMoveToLoadPositionAsync(CancellationToken cancellationToken)
        {
            return Z.CommandMoveToAsync(_loadZ, cancellationToken);
        }

        public Task WaitForLoadPositionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            const double tol = 0.01;
            return Z.WaitForInPositionAsync(_loadZ, tol, timeout, cancellationToken);
        }
    }
}

