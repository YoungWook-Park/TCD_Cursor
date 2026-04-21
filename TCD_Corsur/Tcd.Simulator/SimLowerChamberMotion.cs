using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;

namespace Tcd.Simulator
{
    /// <summary>시뮬레이터 하부 챔버 모션 (U/V/W/Z 4축). ILowerChamberMotion 인터페이스 없이 직접 구현.</summary>
    public sealed class SimLowerChamberMotion
    {
        private readonly double _bondZ;
        private readonly double _loadZ;

        public SimLowerChamberMotion(ITimeProvider time, double bondZ = 100, double loadZ = 0)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            _bondZ = bondZ;
            _loadZ = loadZ;

            U = new SimAxis("U", time, unitsPerSecond: 80);
            V = new SimAxis("V", time, unitsPerSecond: 80);
            W = new SimAxis("W", time, unitsPerSecond: 80);
            Z = new SimAxis("Z", time, unitsPerSecond: 60);
        }

        public SimAxis U { get; }
        public SimAxis V { get; }
        public SimAxis W { get; }
        public SimAxis Z { get; }

        public Task CommandMoveToBondingPositionAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(
                U.CommandMoveToAsync(0, cancellationToken),
                V.CommandMoveToAsync(0, cancellationToken),
                W.CommandMoveToAsync(0, cancellationToken),
                Z.CommandMoveToAsync(_bondZ, cancellationToken)
            );
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
