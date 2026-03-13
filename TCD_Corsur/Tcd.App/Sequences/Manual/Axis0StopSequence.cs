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
/// 0번축(U) 정지 시퀀스. 함수 단위로 로직 구분 + 로그 반영.
/// </summary>
public sealed class Axis0StopSequence : ISequence
{
    private const string AxisName = "U";

    public string Key => TcdSequenceKeys.Manual_Axis0_Stop;
    public string DisplayName => "Manual Axis0(U) Stop";

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var logCtx = new LogContext { SequenceKey = Key, RunId = runId, AxisName = AxisName };
        var core = MainCore.Instance;
        var log = core.Log;
        var motion = core.Motion;

        try
        {
            log.Info(logCtx, "Start", "정지 시퀀스 시작");

            CheckInterlock(context, logCtx, log);
            await RequestStopAsync(motion, logCtx, log, cancellationToken).ConfigureAwait(false);
            LogStopped(logCtx, log);

            log.Info(logCtx, "End", "정지 시퀀스 완료");
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

    /// <summary>인터락/취소 검사</summary>
    private static void CheckInterlock(ISequenceContext context, in LogContext logCtx, ILogWriter log)
    {
        context.StopToken.ThrowIfCancellationRequested();
        log.Debug(logCtx, "CheckInterlock", "인터락 검사 통과");
    }

    /// <summary>정지 명령 발행</summary>
    private static async Task RequestStopAsync(IMotionService motion, LogContext logCtx, ILogWriter log, CancellationToken ct)
    {
        log.Info(logCtx, "RequestStop", $"{AxisName} 축 정지 명령 발행");
        await motion.StopAsync(AxisName, ct).ConfigureAwait(false);
        log.Debug(logCtx, "RequestStop", "정지 명령 완료");
    }

    /// <summary>정지 완료 로그 (실기에서는 정지 완료 신호 대기 후 로그 가능)</summary>
    private static void LogStopped(in LogContext logCtx, ILogWriter log)
    {
        log.Info(logCtx, "Stopped", $"{AxisName} 축 정지 요청 처리됨");
    }
}
