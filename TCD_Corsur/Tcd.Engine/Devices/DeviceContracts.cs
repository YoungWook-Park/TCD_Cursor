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
        // ── 기존 ──────────────────────────────────────────────────────────
        Task<bool> WaitForStageLoadedAsync(
            TimeSpan timeout, CancellationToken cancellationToken);

        // ── 개별 Read / Write ──────────────────────────────────────────────
        Task<bool>  ReadBitAsync(DiBit address, CancellationToken ct);
        Task        WriteBitAsync(DoBit address, bool value, CancellationToken ct);
        Task<short> ReadWordAsync(AiWord address, CancellationToken ct);
        Task        WriteWordAsync(AoWord address, short value, CancellationToken ct);

        // ── IO맵 전체 주기 폴링 ────────────────────────────────────────────
        void StartMonitoring(TimeSpan interval);
        void StopMonitoring();
        event EventHandler<PlcSnapshotArgs> SnapshotUpdated;
    }

    /// <summary>IO맵 전체 스냅샷 이벤트 인자.</summary>
    public sealed class PlcSnapshotArgs : EventArgs
    {
        /// <summary>비트 바이트 배열 (B0~B7, 64비트).</summary>
        public byte[] Bits { get; set; }
        /// <summary>워드 배열 (W0~W31).</summary>
        public short[] Words { get; set; }

        public PlcSnapshotArgs(byte[] bits, short[] words)
        {
            Bits  = bits;
            Words = words;
        }
    }
}
