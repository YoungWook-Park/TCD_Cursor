using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Devices
{
    /// <summary>
    /// TCP 로봇 디바이스 인터페이스.
    /// HMI → 로봇 시뮬레이터(또는 실 로봇 컨트롤러) 통신 계약.
    /// </summary>
    public interface IRobotDevice
    {
        // ── 연결 상태 ──────────────────────────────────────────────────────
        bool IsConnected { get; }

        // ── 로봇 운전 상태 ─────────────────────────────────────────────────
        bool IsRunning          { get; }
        bool IsHome             { get; }
        bool IsError            { get; }
        RobotPosition CurrentPosition { get; }
        string ErrorMessage     { get; }

        /// <summary>
        /// 상태가 변경되었을 때 발생 (이동 완료, 에러, 연결/해제 포함).
        /// 백그라운드 스레드에서 발생할 수 있으므로 UI 바인딩 시 Dispatcher 처리 필요.
        /// </summary>
        event EventHandler<RobotDeviceStateArgs> StateChanged;

        // ── 연결 관리 ──────────────────────────────────────────────────────
        Task ConnectAsync(string host, int port, CancellationToken ct = default);
        void Disconnect();

        // ── 커맨드 ──────────────────────────────────────────────────────────
        /// <summary>포지션 개별 속도 설정 (0-100 %). Move 전에 호출.</summary>
        Task<bool> SetVelocityAsync(
            RobotPosition position, int pct, CancellationToken ct = default);

        /// <summary>지정 포지션으로 이동 시작. 서버 Ack 수신 후 반환.</summary>
        Task<bool> MoveAsync(
            RobotPosition position, CancellationToken ct = default);

        Task<bool> StopAsync(CancellationToken ct = default);

        /// <summary>
        /// 지정 포지션 도달 + IsRunning=false 까지 폴링 대기.
        /// timeout 초과 시 TimeoutException.
        /// </summary>
        Task WaitForPositionAsync(
            RobotPosition position, TimeSpan timeout, CancellationToken ct);
    }

    /// <summary>StateChanged 이벤트 데이터</summary>
    public sealed class RobotDeviceStateArgs : EventArgs
    {
        public bool IsConnected              { get; set; }
        public bool IsRunning                { get; set; }
        public bool IsHome                   { get; set; }
        public bool IsError                  { get; set; }
        public RobotPosition CurrentPosition { get; set; }
        public string ErrorMessage           { get; set; }
    }
}
