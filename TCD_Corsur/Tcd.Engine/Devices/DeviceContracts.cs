using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Materials;

namespace Tcd.Devices
{
    public enum RobotPosition
    {
        // ── 인프로세스 SimRobot / 자동 시퀀스 호환 (변경 금지) ────────────
        Home             = 0,
        Stage            = 1,   // 레거시: Auto/SemiAuto 시퀀스에서만 사용
        UpperChamberLoad = 2,   // 레거시: Auto/SemiAuto 시퀀스에서만 사용
        LowerChamberLoad = 3,   // 레거시: Auto/SemiAuto 시퀀스에서만 사용

        // ── TCP 로봇 디바이스 포지션 (Manual / SemiAuto 신규) ─────────────
        Ready              = 10, // 초기화 전용 (SetVelocity 기준 포지션)
        UpperStageWait     = 11, // 상부 스테이지 진입 전 대기
        UpperStageContact  = 12, // 상부 스테이지 접촉 (픽/플레이스)
        LowerStageWait     = 13, // 하부 스테이지 진입 전 대기
        LowerStageContact  = 14, // 하부 스테이지 접촉
        UpperChamberWait   = 15, // 상부 챔버 진입 전 대기  ← Z Ready 인터락
        UpperChamberContact= 16, // 상부 챔버 접촉          ← Z Ready 인터락
        LowerChamberWait   = 17, // 하부 챔버 진입 전 대기  ← Z Ready 인터락
        LowerChamberContact= 18, // 하부 챔버 접촉          ← Z Ready 인터락
        Peel               = 19,
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
