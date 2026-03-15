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

/// <summary>V축 증분 이동. 파라미터는 이동량(delta, double).</summary>
public sealed class ManualMotorVIncMoveSequence : ISequence
{
    #region Variables

    private const string AxisName = AxisDefine.V;

    public string Key => TcdSequenceKeys.Manual_Motor_V_IncMove;
    public string DisplayName => "Manual Motor V IncMove";

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
            double delta = Param is double d ? d : (Param is IConvertible c ? c.ToDouble(null) : 0);
            log.Info(core.LogContext, "Start", "IncMove 시작", new Dictionary<string, object> { ["Delta"] = delta });
            CheckInterlock(context);
            await core.Motion.IncMoveAsync(AxisName, delta, cancellationToken).ConfigureAwait(false);
            log.Info(core.LogContext, "End", "IncMove 정상 완료", new Dictionary<string, object> { ["Delta"] = delta });
            return SequenceResult.Success();
        }
        catch (OperationCanceledException)
        {
            log.Warn(core.LogContext, "Stopped", "시퀀스 취소됨");
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
