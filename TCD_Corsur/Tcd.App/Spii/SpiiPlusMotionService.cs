using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Devices;

namespace Tcd.App.Spii;

/// <summary>
/// ascpl 0번 버퍼(ABS/STOP) + 9번 모니터링 버퍼를 사용하는 IMotionService 구현.
/// - 축 인덱스: U=0, V=1, W=2, ZLower=3, ZUpper=4 (AxisDefine.InOrder)
/// - 타겟/속도/가감속/저크: PC_ACS_* 배열 사용
/// - 명령 플래그: CMD_ABS_MOVE(axis), CMD_STOP(axis)
/// - 상태: ACS_PC_IS_MOVE_AXISn, ACS_PC_IS_FAULT_AXISn, ACS_PC_CURRENT_POS_AXISn
/// - 백그라운드 모니터링 태스크가 ACS 상태를 주기적으로 읽어 캐시에 반영; ViewModel은 캐시만 참조.
/// </summary>
public sealed class SpiiPlusMotionService : IMotionService, IAxisStateProvider, IDisposable
{
    private const int AxisCount = 5;
    private const int MonitorIntervalMs = 120;

    private readonly SpiiPlusConnection _conn;
    private readonly AxisState[] _cache;
    private readonly object _cacheLock = new object();
    private readonly CancellationTokenSource _monitorCts = new CancellationTokenSource();
    private Task? _monitorTask;
    private bool _disposed;

    public SpiiPlusMotionService(string ipAddress)
    {
        _conn = new SpiiPlusConnection(ipAddress);

        // 모니터링 버퍼(9번) 시작
        _conn.WriteIntAt(SpiiDefine.ON_MONITORING_FLAG, 0, 1);
        _conn.RunBuffer(9);

        _cache = new AxisState[AxisCount];
        for (int i = 0; i < AxisCount; i++)
        {
            _cache[i] = new AxisState { AxisName = AxisDefine.InOrder[i] };
        }

        _monitorTask = Task.Run(() => MonitorLoopAsync(_monitorCts.Token));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _monitorCts.Cancel();
        try { _monitorTask?.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
        _conn.Dispose();
    }

    /// <summary>캐시된 축 상태 반환 (디바이스 직접 호출 없음).</summary>
    public AxisState GetAxisState(string axisName)
    {
        int idx = AxisNameToIndex(axisName);
        lock (_cacheLock)
        {
            var c = _cache[idx];
            return new AxisState
            {
                AxisName = c.AxisName,
                Position = c.Position,
                IsMoving = c.IsMoving,
                IsFault = c.IsFault,
                IsHome = c.IsHome,
                IsServoOn = c.IsServoOn
            };
        }
    }

    /// <summary>전체 축 스냅샷 (순서: U, V, W, ZLower, ZUpper).</summary>
    public IReadOnlyList<AxisState> GetSnapshot()
    {
        lock (_cacheLock)
        {
            var list = new List<AxisState>(AxisCount);
            for (int i = 0; i < AxisCount; i++)
            {
                var c = _cache[i];
                list.Add(new AxisState
                {
                    AxisName = c.AxisName,
                    Position = c.Position,
                    IsMoving = c.IsMoving,
                    IsFault = c.IsFault,
                    IsHome = c.IsHome
                });
            }
            return list;
        }
    }

    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                for (int i = 0; i < AxisCount; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    double pos = _conn.ReadReal($"{SpiiDefine.ACS_PC_CURRENT_POS_AXIS}{i}");
                    int isMove = _conn.ReadInt($"{SpiiDefine.ACS_PC_IS_MOVE_AXIS}{i}");
                    int isFault = _conn.ReadInt($"{SpiiDefine.ACS_PC_IS_FAULT_AXIS}{i}");
                    int isHome = _conn.ReadInt($"{SpiiDefine.ACS_PC_IS_HOME_AXIS}{i}");
                    int isServoOn = _conn.ReadInt($"{SpiiDefine.ACS_PC_IS_ENABLED_AXIS}{i}");
                    lock (_cacheLock)
                    {
                        _cache[i].Position = pos;
                        _cache[i].IsMoving = isMove != 0;
                        _cache[i].IsFault = isFault != 0;
                        _cache[i].IsHome = isHome != 0;
                        _cache[i].IsServoOn = isServoOn != 0;
                    }
                }
            }
            catch (Exception)
            {
                // 연결 끊김 등: 캐시는 그대로 두고 다음 주기에 재시도
            }
            await Task.Delay(MonitorIntervalMs, ct).ConfigureAwait(false);
        }
    }

    public Task AbsMoveAsync(string axis, double targetPosition, CancellationToken cancellationToken)
        => RunAbsMoveAsync(axis, targetPosition, cancellationToken);

    public Task IncMoveAsync(string axis, double delta, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            double current = GetAxisState(axis).Position;
            await RunAbsMoveAsync(axis, current + delta, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task JogAsync(string axis, double velocity, CancellationToken cancellationToken)
    {
        return RunJogAsync(axis, velocity, cancellationToken);
    }

    public Task StopAsync(string axis, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            int idx = AxisNameToIndex(axis);
            // RD_Halt_CMD(axis) = 1 설정 → ascpl: ON RD_Halt_CMD(iAXIS)=1 & PST(1).#RUN=0 → HALT(iAXIS)
            _conn.WriteIntAt(SpiiDefine.RD_Halt_CMD, idx, 1);
        }, cancellationToken);
    }

    public Task HomeAsync(string axis, CancellationToken cancellationToken)
    {
        return RunHomeAsync(axis, cancellationToken);
    }

    public Task FaultClearAsync(string axis, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            int idx = AxisNameToIndex(axis);
            // ascpl: ON RD_Fcle_CMD(iAXIS)=1 → FCLEAR(iAXIS)
            _conn.WriteIntAt(SpiiDefine.RD_Fcle_CMD, idx, 1);
        }, cancellationToken);
    }

    public Task ServoOnAsync(string axis, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            int idx = AxisNameToIndex(axis);
            _conn.WriteIntAt(SpiiDefine.RD_Ena_CMD, idx, 1);
        }, cancellationToken);
    }

    public Task ServoOffAsync(string axis, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            int idx = AxisNameToIndex(axis);
            _conn.WriteIntAt(SpiiDefine.RD_Disable_CMD, idx, 1);
        }, cancellationToken);
    }

    /// <summary>
    /// 캐시된 현재 위치 반환 (모니터링 태스크가 주기적으로 갱신). 레거시 호환용.
    /// </summary>
    public double GetActualPosition(string axisName)
    {
        return GetAxisState(axisName).Position;
    }

    // -------- 내부 구현 --------

    private async Task RunAbsMoveAsync(string axisName, double target, CancellationToken ct)
    {
        int axis = AxisNameToIndex(axisName);

        // Servo ON 요청
        _conn.WriteIntAt(SpiiDefine.RD_Ena_CMD, axis, 1);

        var recipe = MainCore.Instance.Recipes.Current;
        double vel = recipe?.MotionVelocity ?? SpiiDefine.DefaultVelocity;
        double acc = recipe?.MotionAcc ?? SpiiDefine.DefaultAcc;
        double dec = recipe?.MotionDec ?? SpiiDefine.DefaultDec;
        double jerk = recipe?.MotionJerk ?? SpiiDefine.DefaultJerk;

        _conn.WriteRealAt(SpiiDefine.PC_ACS_DISTANCE, axis, target);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_VELOCITY, axis, vel);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_ACC, axis, acc);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_DEC, axis, dec);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_JERK, axis, jerk);

        // 2) 명령 플래그: RD_Abs_CMD(axis) = 1
        _conn.WriteIntAt(SpiiDefine.RD_Abs_CMD, axis, 1);

        // 3) 모션 완료/에러 대기
        string isMoveVar = $"{SpiiDefine.ACS_PC_IS_MOVE_AXIS}{axis}";
        string isFaultVar = $"{SpiiDefine.ACS_PC_IS_FAULT_AXIS}{axis}";

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            int isFault = _conn.ReadInt(isFaultVar);
            int isMove = _conn.ReadInt(isMoveVar);

            if (isFault != 0)
                throw new InvalidOperationException($"SPII AbsMove Fault axis={axisName}");

            if (isMove == 0)
                break;

            await Task.Delay(10, ct).ConfigureAwait(false);
        }
    }

    private async Task RunJogAsync(string axisName, double velocity, CancellationToken ct)
    {
        int axis = AxisNameToIndex(axisName);
        if (Math.Abs(velocity) < 1e-6) return;

        // Servo ON
        _conn.WriteIntAt(SpiiDefine.RD_Ena_CMD, axis, 1);

        var recipe = MainCore.Instance.Recipes.Current;
        double acc = recipe?.MotionAcc ?? SpiiDefine.DefaultAcc;
        double dec = recipe?.MotionDec ?? SpiiDefine.DefaultDec;
        double jerk = recipe?.MotionJerk ?? SpiiDefine.DefaultJerk;

        _conn.WriteRealAt(SpiiDefine.PC_ACS_VELOCITY, axis, Math.Abs(velocity));
        _conn.WriteRealAt(SpiiDefine.PC_ACS_ACC, axis, acc);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_DEC, axis, dec);
        _conn.WriteRealAt(SpiiDefine.PC_ACS_JERK, axis, jerk);

        if (velocity > 0)
            _conn.WriteIntAt(SpiiDefine.RD_pJog_CMD, axis, 1);
        else
            _conn.WriteIntAt(SpiiDefine.RD_nJog_CMD, axis, 1);

        string isFaultVar = $"{SpiiDefine.ACS_PC_IS_FAULT_AXIS}{axis}";

        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                int isFault = _conn.ReadInt(isFaultVar);
                if (isFault != 0)
                    throw new InvalidOperationException($"SPII Jog Fault axis={axisName}");

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            // Jog 중단: 플래그 0으로 내리면 ascpl에서 HALT 수행
            _conn.WriteIntAt(SpiiDefine.RD_pJog_CMD, axis, 0);
            _conn.WriteIntAt(SpiiDefine.RD_nJog_CMD, axis, 0);
        }
    }

    private async Task RunHomeAsync(string axisName, CancellationToken ct)
    {
        int axis = AxisNameToIndex(axisName);

        _conn.WriteIntAt(SpiiDefine.RD_Ena_CMD, axis, 1);
        _conn.WriteIntAt(SpiiDefine.RD_Home_CMD, axis, 1);

        string isHomeVar = $"{SpiiDefine.ACS_PC_IS_HOME_AXIS}{axis}";
        string isFaultVar = $"{SpiiDefine.ACS_PC_IS_FAULT_AXIS}{axis}";

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            int isFault = _conn.ReadInt(isFaultVar);
            int isHome = _conn.ReadInt(isHomeVar);

            if (isFault != 0)
                throw new InvalidOperationException($"SPII Home Fault axis={axisName}");

            if (isHome != 0)
                break;

            await Task.Delay(20, ct).ConfigureAwait(false);
        }
    }

    private int AxisNameToIndex(string axisName)
    {
        if (axisName == null) throw new ArgumentNullException(nameof(axisName));
        var key = axisName.Trim();
        if (string.Equals(key, AxisDefine.U, StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(key, AxisDefine.V, StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(key, AxisDefine.W, StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(key, AxisDefine.ZLower, StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(key, AxisDefine.ZUpper, StringComparison.OrdinalIgnoreCase)) return 4;
        throw new ArgumentOutOfRangeException(nameof(axisName), axisName, "Unknown axis name (expected U/V/W/ZLower/ZUpper)");
    }
}

