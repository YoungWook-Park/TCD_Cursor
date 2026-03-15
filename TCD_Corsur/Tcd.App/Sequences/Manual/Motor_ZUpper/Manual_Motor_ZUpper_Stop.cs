using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual.Motor_ZUpper;

/// <summary>ZUpper축 정지.</summary>
public sealed class ManualMotorZUpperStopSequence : ISequence
{
    #region Variables

    private const string AxisName = AxisDefine.ZUpper;

    public string Key => TcdSequenceKeys.Manual_Motor_ZUpper_Stop;
    public string DisplayName => "Manual Motor ZUpper Stop";

    public object Param { get; set; } = null!;

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = AxisName };

        try
        {
            core.Log.Info(core.LogContext, "Start", "정지 시퀀스 시작");
            CheckInterlock(context);
            await core.Motion.StopAsync(AxisName, cancellationToken).ConfigureAwait(false);
            core.Log.Info(core.LogContext, "End", "정지 시퀀스 완료");
            return SequenceResult.Success();
        }
        catch (OperationCanceledException)
        {
            core.Log.Warn(core.LogContext, "Stopped", "시퀀스 취소됨");
            return SequenceResult.Stopped();
        }
        catch (Exception ex)
        {
            core.Log.Error(core.LogContext, "Error", ex.Message, null, ex);
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
