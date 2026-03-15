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

namespace Tcd.App.Sequences.Manual.Motor_W;

/// <summary>W축 절대 위치 이동. 목표값은 레시피(축키 W)에서 참조.</summary>
public sealed class ManualMotorWAbsMoveSequence : ISequence
{
    #region Variables

    private const string AxisName = AxisDefine.W;

    public string Key => TcdSequenceKeys.Manual_Motor_W_AbsMove;
    public string DisplayName => "Manual Motor W AbsMove";

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
            double target = GetTargetFromRecipe();
            log.Info(core.LogContext, "Start", "시퀀스 시작 (레시피 목표값 사용)", new Dictionary<string, object> { ["Target"] = target });
            CheckInterlock(context);
            await core.Motion.AbsMoveAsync(AxisName, target, cancellationToken).ConfigureAwait(false);
            log.Info(core.LogContext, "End", "시퀀스 정상 완료", new Dictionary<string, object> { ["Target"] = target });
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

    private static double GetTargetFromRecipe()
    {
        var recipe = MainCore.Instance.Recipes.Current;
        return recipe?.GetAxis(AxisName, 0) ?? 0;
    }

    #endregion
}
