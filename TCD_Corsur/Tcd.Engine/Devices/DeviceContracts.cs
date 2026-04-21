using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Materials;

namespace Tcd.Devices
{
    public enum RobotPosition
    {
        // ── 기존 인프로세스 SimRobot / 자동 시퀀스 호환 ──────────────────
        Home             = 0,
        Stage            = 1,   // 스테이지 영역 (기존 시퀀스 공용)
        UpperChamberLoad = 2,
        LowerChamberLoad = 3,

        // ── TCP 로봇 시뮬레이터 확장 포지션 ──────────────────────────────
        Ready                   = 10,
        S1_PickupWait           = 11,
        S1_Pick                 = 12,
        S2_PickupWait           = 13,
        S2_Pick                 = 14,
        UpperChamber_PickupWait = 15,
        UpperChamber_Pick       = 16,
        LowerChamber_PickupWait = 17,
        LowerChamber_Pick       = 18,
        Peel                    = 19,
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
