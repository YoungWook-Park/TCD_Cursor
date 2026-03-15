using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual.Motor_V;

/// <summary>V축 조그. 파라미터는 속도(velocity, double, 부호가 방향). 취소 시까지 동작.</summary>
public sealed class ManualMotorVJogMoveSequence : ISequence
{
    #region Variables

    private const string AxisName = AxisDefine.V;

    public string Key => TcdSequenceKeys.Manual_Motor_V_JogMove;
    public string DisplayName => "Manual Motor V JogMove";

    public object Param { get; set; } = null!;

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = AxisName };

        var log = core.Log;
        try
        {
            double velocity = Param is double v ? v : (Param is IConvertible c ? c.ToDouble(null) : 0);
            log.Info(core.LogContext, "Start", "JogMove 시작 (취소 시 정지)", new Dictionary<string, object> { ["Velocity"] = velocity });
            CheckInterlock(context);
            await core.Motion.JogAsync(AxisName, velocity, cancellationToken).ConfigureAwait(false);
            log.Info(core.LogContext, "End", "JogMove 종료(취소됨 또는 완료)");
            return SequenceResult.Success();
        }
        catch (OperationCanceledException)
        {
            log.Warn(core.LogContext, "Stopped", "조그 취소됨");
            return SequenceResult.Stopped();
        }
        catch (Exception ex)
        {
            log.Error(core.LogContext, "Error", ex.Message, null, ex);
            context.Alarms.Raise(new Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail(ex.Message);
        }
    }

    private static void CheckInterlock(ISequenceContext context)
    {
        context.StopToken.ThrowIfCancellationRequested();
    }

    #endregion
}
