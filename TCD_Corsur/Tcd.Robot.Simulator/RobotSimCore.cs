using System.Text.Json;
using Tcd.Robot.Simulator.Protocol;
using Tcd.Robot.Simulator.Teaching;

namespace Tcd.Robot.Simulator;

/// <summary>
/// 로봇 시뮬레이터 핵심 상태 머신.
/// 스레드 세이프: 모든 공유 상태는 _lock으로 보호.
/// </summary>
public sealed class RobotSimCore
{
    #region Variable

    private readonly object _lock = new();
    private readonly Dictionary<int, TeachPoint> _teachPoints;

    private int     _currentPos    = 0;   // Home
    private bool    _isRunning     = false;
    private bool    _isError       = false;
    private string? _errorMessage  = null;
    private CancellationTokenSource? _moveCts;

    // 상태 변화 / 이동 완료 시 구독자에게 Push
    public event Action<RobotResponse>? StatePushed;

    #endregion

    #region Constructor

    public RobotSimCore()
    {
        _teachPoints = DefaultTeachTable.Build();
    }

    #endregion

    #region Public API

    /// <summary>현재 상태 스냅샷 반환 (heartbeat / GetState용)</summary>
    public RobotResponse GetCurrentState()
    {
        lock (_lock)
            return BuildStateResponse();
    }

    /// <summary>수신된 요청을 처리하고 즉시 Ack 반환. 이동은 비동기로 진행.</summary>
    public RobotResponse HandleRequest(RobotRequest req)
    {
        return req.T switch
        {
            MsgType.GetState    => GetCurrentState(),
            MsgType.SetVelocity => HandleSetVelocity(req),
            MsgType.Move        => HandleMove(req),
            MsgType.Stop        => HandleStop(),
            MsgType.SetTeach    => HandleSetTeach(req),
            _                   => Ack(req.T, false, $"Unknown command '{req.T}'"),
        };
    }

    #endregion

    #region Command Handlers

    private RobotResponse HandleSetVelocity(RobotRequest req)
    {
        lock (_lock)
        {
            if (!_teachPoints.TryGetValue(req.Pos, out var tp))
                return Ack(MsgType.SetVelocity, false, $"Unknown position {req.Pos}");

            tp.VelocityPct = Math.Clamp(req.Pct, 1, 100);
            Console.WriteLine($"[Core] SetVelocity pos={req.Pos} pct={tp.VelocityPct}");
            return Ack(MsgType.SetVelocity, true);
        }
    }

    private RobotResponse HandleMove(RobotRequest req)
    {
        lock (_lock)
        {
            // 에러 인터락
            if (_isError)
                return Ack(MsgType.Move, false, "Error state. Stop and clear first.");

            // 이미 이동 중
            if (_isRunning)
                return Ack(MsgType.Move, false, "Already running.");

            // 티칭 위치 존재 확인
            if (!_teachPoints.TryGetValue(req.Pos, out _))
                return Ack(MsgType.Move, false, $"Unknown position {req.Pos}");

            // ── 인터락: Home(0) 또는 Ready(10) 위치가 아니면
            //            Home/Ready 이외의 목적지로 이동 불가 ──────────────
            bool atSafe = _currentPos == 0 || _currentPos == 10;
            bool goSafe = req.Pos   == 0 || req.Pos    == 10;
            if (!atSafe && !goSafe)
                return Ack(MsgType.Move, false,
                    $"Must be at Home(0) or Ready(10) before moving. " +
                    $"Current={_currentPos}. Move to Ready first.");

            _isRunning = true;
            CancelMove();
            _moveCts = new CancellationTokenSource();
            var ct = _moveCts.Token;
            var target = req.Pos;

            _ = Task.Run(() => RunMoveAsync(target, ct));
            Console.WriteLine($"[Core] Move start: {_currentPos} → {target}");
            return Ack(MsgType.Move, true);
        }
    }

    private RobotResponse HandleStop()
    {
        lock (_lock)
        {
            CancelMove();
            _isRunning = false;
            Console.WriteLine("[Core] Stop");
        }
        Push(BuildStateResponse());
        return Ack(MsgType.Stop, true);
    }

    private RobotResponse HandleSetTeach(RobotRequest req)
    {
        lock (_lock)
        {
            if (!_teachPoints.TryGetValue(req.Pos, out var tp))
                return Ack(MsgType.SetTeach, false, $"Unknown position {req.Pos}");

            tp.X = req.X; tp.Y = req.Y; tp.Theta = req.Theta;
            Console.WriteLine($"[Core] SetTeach pos={req.Pos} X={req.X} Y={req.Y} θ={req.Theta}");
            return Ack(MsgType.SetTeach, true);
        }
    }

    #endregion

    #region Move Simulation

    private async Task RunMoveAsync(int targetPos, CancellationToken ct)
    {
        try
        {
            int    delayMs;
            lock (_lock)
            {
                var from    = _teachPoints[_currentPos];
                var to      = _teachPoints[targetPos];
                double dist = Math.Sqrt(
                    Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
                double speed = DefaultTeachTable.MaxSpeedMmPerSec
                               * Math.Max(to.VelocityPct, 1) / 100.0;
                delayMs = Math.Max(
                    (int)(dist / speed * 1000.0),
                    DefaultTeachTable.MinMoveMs);
            }

            await Task.Delay(delayMs, ct).ConfigureAwait(false);

            lock (_lock)
            {
                _currentPos = targetPos;
                _isRunning  = false;
            }

            Console.WriteLine($"[Core] Arrived at {targetPos}");

            // Arrived Push (이동 완료 알림)
            Push(new RobotResponse
            {
                T   = MsgType.Arrived,
                Pos = targetPos,
                Connected = true,
                Running   = false,
                Home      = targetPos == 0,
                Error     = false,
            });
            // State Push (상태 갱신)
            Push(BuildStateResponse());
        }
        catch (OperationCanceledException)
        {
            lock (_lock) { _isRunning = false; }
            Console.WriteLine("[Core] Move cancelled");
            Push(BuildStateResponse());
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _isRunning    = false;
                _isError      = true;
                _errorMessage = ex.Message;
            }
            Console.WriteLine($"[Core] Move error: {ex.Message}");
            Push(BuildStateResponse());
        }
    }

    #endregion

    #region Helpers

    private void CancelMove()
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;
    }

    private RobotResponse BuildStateResponse()
    {
        return new RobotResponse
        {
            T         = MsgType.State,
            Connected = true,
            Running   = _isRunning,
            Home      = _currentPos == 0,
            Error     = _isError,
            Pos       = _currentPos,
            ErrMsg    = _errorMessage,
        };
    }

    private void Push(RobotResponse resp) => StatePushed?.Invoke(resp);

    private static RobotResponse Ack(string cmd, bool ok, string? err = null) =>
        new() { T = MsgType.Ack, Cmd = cmd, Ok = ok, Err = err };

    #endregion
}
