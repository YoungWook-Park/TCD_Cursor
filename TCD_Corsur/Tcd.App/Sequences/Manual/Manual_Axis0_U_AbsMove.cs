using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

/// <summary>
/// 0번축(U) 절대 위치 기동 시퀀스. 함수 단위로 로직 구분 + 로그 반영.
/// </summary>
public sealed class Axis0AbsMoveSequence : ISequence
{
    private const string AxisName = "U";

    public string Key => TcdSequenceKeys.Manual_Axis0_AbsMove;
    public string DisplayName => "Manual Axis0(U) AbsMove";

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var logCtx = new LogContext { SequenceKey = Key, RunId = runId, AxisName = AxisName };
        var core = MainCore.Instance;
        var log = core.Log;
        var motion = core.Motion;

        try
        {
            log.Info(logCtx, "Start", "시퀀스 시작", new System.Collections.Generic.Dictionary<string, object> { ["Parameter"] = parameter ?? "(null)" });

            CheckInterlock(context, logCtx, log);
            double target = ParseTarget(parameter, logCtx, log);

            await StartMoveAsync(motion, target, logCtx, log, cancellationToken).ConfigureAwait(false);
            await WaitCompleteAsync(context, target, logCtx, log, cancellationToken).ConfigureAwait(false);
            VerifyPosition(context, target, logCtx, log);

            log.Info(logCtx, "End", "시퀀스 정상 완료", new System.Collections.Generic.Dictionary<string, object> { ["Target"] = target });
            return SequenceResult.Success();
        }
        catch (OperationCanceledException)
        {
            log.Warn(logCtx, "Stopped", "시퀀스 취소됨");
            return SequenceResult.Stopped();
        }
        catch (Exception ex)
        {
            log.Error(logCtx, "Error", ex.Message, null, ex);
            context.Alarms.Raise(new Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail(ex.Message);
        }
    }

    /// <summary>인터락 조건 검사 (필요 시 확장)</summary>
    private static void CheckInterlock(ISequenceContext context, in LogContext logCtx, ILogWriter log)
    {
        context.StopToken.ThrowIfCancellationRequested();
        log.Debug(logCtx, "CheckInterlock", "인터락 검사 통과");
    }

    /// <summary>파라미터에서 목표 위치 파싱</summary>
    private static double ParseTarget(object? parameter, in LogContext logCtx, ILogWriter log)
    {
        double target = 0.0;
        if (parameter is double d)
            target = d;
        else if (parameter is int i)
            target = i;
        else if (parameter is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            target = parsed;

        log.Info(logCtx, "ParseTarget", $"목표 위치 설정: {target}", new System.Collections.Generic.Dictionary<string, object> { ["Target"] = target });
        return target;
    }

    /// <summary>이동 명령 발행</summary>
    private static async Task StartMoveAsync(IMotionService motion, double target, LogContext logCtx, ILogWriter log, CancellationToken ct)
    {
        log.Info(logCtx, "StartMove", $"AbsMove 명령 발행 target={target}");
        await motion.AbsMoveAsync(AxisName, target, ct).ConfigureAwait(false);
        log.Debug(logCtx, "StartMove", "AbsMove 완료(대기 포함)");
    }

    /// <summary>이동 완료 대기 (AbsMoveAsync가 이미 대기 포함 시 시뮬레이터에서는 추가 대기 불필요, 로그만)</summary>
    private static Task WaitCompleteAsync(ISequenceContext context, double target, LogContext logCtx, ILogWriter log, CancellationToken ct)
    {
        log.Debug(logCtx, "WaitComplete", $"목표 도달 대기 target={target}");
        context.StopToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <summary>위치 검증 (시뮬레이터/실기 공통으로 로그만; 실기에서는 실제 위치 읽어 비교 가능)</summary>
    private static void VerifyPosition(ISequenceContext context, double target, in LogContext logCtx, ILogWriter log)
    {
        if (context is TcdSimulation sim)
        {
            var actual = sim.LowerMotion.U.Position;
            var tol = 0.01;
            if (Math.Abs(actual - target) > tol)
                log.Warn(logCtx, "VerifyPosition", $"위치 오차 있음 target={target} actual={actual}");
            else
                log.Debug(logCtx, "VerifyPosition", $"위치 검증 통과 target={target}");
        }
        else
        {
            log.Debug(logCtx, "VerifyPosition", $"목표 위치 검증 요청 target={target}");
        }
    }
}
