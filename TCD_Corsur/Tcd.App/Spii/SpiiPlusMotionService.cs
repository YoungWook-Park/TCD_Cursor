using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Devices;

namespace Tcd.App.Spii;

/// <summary>
/// ascpl 0번 버퍼(ABS/STOP) + 9번 모니터링 버퍼를 사용하는 IMotionService 구현.
/// - 축 인덱스: U=0, V=1, W=2, ZUpper=3, ZLower=4
/// - 타겟/속도/가감속/저크: PC_ACS_* 배열 사용
/// - 명령 플래그: CMD_ABS_MOVE(axis), CMD_STOP(axis)
/// - 상태: ACS_PC_IS_MOVE_AXISn, ACS_PC_IS_FAULT_AXISn, ACS_PC_CURRENT_POS_AXISn
/// </summary>
public sealed class SpiiPlusMotionService : IMotionService, IDisposable
{
    private readonly SpiiPlusConnection _conn;

    public SpiiPlusMotionService(string ipAddress)
    {
        _conn = new SpiiPlusConnection(ipAddress);

        // 모니터링 버퍼(9번) 시작
        _conn.WriteIntAt("ON_MONITORING_FLAG", 0, 1);
        _conn.RunBuffer(9);
    }

    public void Dispose()
    {
        _conn.Dispose();
    }

    public Task AbsMoveAsync(string axis, double targetPosition, CancellationToken cancellationToken)
        => RunAbsMoveAsync(axis, targetPosition, cancellationToken);

    public Task IncMoveAsync(string axis, double delta, CancellationToken cancellationToken)
    {
        // 현재 위치 + delta 를 목표로 AbsMove 로 처리
        return Task.Run(async () =>
        {
            int idx = AxisNameToIndex(axis);
            var current = ReadCurrentPosition(idx);
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
            _conn.WriteIntAt("RD_Halt_CMD", idx, 1);
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
            _conn.WriteIntAt("RD_Fcle_CMD", idx, 1);
        }, cancellationToken);
    }

    /// <summary>
    /// 모니터링 버퍼에서 지정 축의 현재 위치를 읽어서 반환합니다.
    /// (ACS_PC_CURRENT_POS_AXISn)
    /// </summary>
    public double GetActualPosition(string axisName)
    {
        int axis = AxisNameToIndex(axisName);
        return ReadCurrentPosition(axis);
    }

    // -------- 내부 구현 --------

    private async Task RunAbsMoveAsync(string axisName, double target, CancellationToken ct)
    {
        int axis = AxisNameToIndex(axisName);

        // Servo ON 요청
        _conn.WriteIntAt("RD_Ena_CMD", axis, 1);

        // 1) 목표 위치/속도/가감속/저크 설정
        _conn.WriteRealAt("PC_ACS_DISTANCE", axis, target);

        // velocity/acc/dec/jerk 가 0이면 ascpl에서 STOP 되므로, 최소값을 넣어줍니다.
        _conn.WriteRealAt("PC_ACS_VELOCITY", axis, 100); // TODO: 설정값으로 대체
        _conn.WriteRealAt("PC_ACS_ACC", axis, 1000);
        _conn.WriteRealAt("PC_ACS_DEC", axis, 1000);
        _conn.WriteRealAt("PC_ACS_JERK", axis, 1000);

        // 2) 명령 플래그: RD_Abs_CMD(axis) = 1
        _conn.WriteIntAt("RD_Abs_CMD", axis, 1);

        // 3) 모션 완료/에러 대기: 모니터링 버퍼(9번)가 ACS_PC_IS_MOVE_AXISn / ACS_PC_IS_FAULT_AXISn 을 갱신
        string isMoveVar = $"ACS_PC_IS_MOVE_AXIS{axis}";
        string isFaultVar = $"ACS_PC_IS_FAULT_AXIS{axis}";

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
        _conn.WriteIntAt("RD_Ena_CMD", axis, 1);

        // 속도/가감속/저크 설정
        _conn.WriteRealAt("PC_ACS_VELOCITY", axis, Math.Abs(velocity));
        _conn.WriteRealAt("PC_ACS_ACC", axis, 1000);
        _conn.WriteRealAt("PC_ACS_DEC", axis, 1000);
        _conn.WriteRealAt("PC_ACS_JERK", axis, 1000);

        // 방향에 따라 +/- Jog 명령 플래그 설정
        if (velocity > 0)
        {
            _conn.WriteIntAt("RD_pJog_CMD", axis, 1);
        }
        else
        {
            _conn.WriteIntAt("RD_nJog_CMD", axis, 1);
        }

        string isFaultVar = $"ACS_PC_IS_FAULT_AXIS{axis}";

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
            _conn.WriteIntAt("RD_pJog_CMD", axis, 0);
            _conn.WriteIntAt("RD_nJog_CMD", axis, 0);
        }
    }

    private async Task RunHomeAsync(string axisName, CancellationToken ct)
    {
        int axis = AxisNameToIndex(axisName);

        // Servo ON
        _conn.WriteIntAt("RD_Ena_CMD", axis, 1);

        // Home 명령 플래그
        _conn.WriteIntAt("RD_Home_CMD", axis, 1);

        string isHomeVar = $"ACS_PC_IS_HOME_AXIS{axis}";
        string isFaultVar = $"ACS_PC_IS_FAULT_AXIS{axis}";

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

    private double ReadCurrentPosition(int axis)
    {
        string varName = axis switch
        {
            0 => "ACS_PC_CURRENT_POS_AXIS0",
            1 => "ACS_PC_CURRENT_POS_AXIS1",
            2 => "ACS_PC_CURRENT_POS_AXIS2",
            3 => "ACS_PC_CURRENT_POS_AXIS3",
            4 => "ACS_PC_CURRENT_POS_AXIS4",
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis index")
        };
        return _conn.ReadReal(varName);
    }

    private int AxisNameToIndex(string axisName)
    {
        if (axisName == null) throw new ArgumentNullException(nameof(axisName));
        axisName = axisName.Trim().ToUpperInvariant();

        return axisName switch
        {
            "U" => 0,
            "V" => 1,
            "W" => 2,
            "ZUPPER" => 3,
            "ZLOWER" => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(axisName), axisName, "Unknown axis name (expected U/V/W/ZUpper/ZLower)")
        };
    }
}

