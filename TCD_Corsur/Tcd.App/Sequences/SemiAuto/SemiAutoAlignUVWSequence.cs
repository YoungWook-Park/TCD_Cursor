using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: UVW 정렬. 인터락: 로봇이 홈 위치에 있어야 함. U/V/W 동시 명령 후 대기.</summary>
public sealed class SemiAutoAlignUVWSequence : ISequence
{
    #region Variables

    public string Key => TcdSequenceKeys.SEMI_AlignUVW;
    public string DisplayName => "SEMI: Align UVW";
    public object Param { get; set; } = null!;

    private static readonly TimeSpan AxisWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid() };

        var sim = core.Simulation;
        var mgr = core.Sequences;

        if (sim.Robot.CurrentPosition != RobotPosition.Home)
        {
            context.Alarms.Raise(new Alarm("ROBOT_NOT_AT_HOME", "UVW align interlock: Robot must be at home position.", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail("Robot must be at home before UVW align.");
        }

        // fork: command U, V, W simultaneously
        var cmdU = mgr.RunAsync(TcdSequenceKeys.AxisU_Command_Zero, context, null, cancellationToken);
        var cmdV = mgr.RunAsync(TcdSequenceKeys.AxisV_Command_Zero, context, null, cancellationToken);
        var cmdW = mgr.RunAsync(TcdSequenceKeys.AxisW_Command_Zero, context, null, cancellationToken);

        var rU = await cmdU.ConfigureAwait(false);
        if (rU.Status != SequenceStatus.Succeeded) return rU;
        var rV = await cmdV.ConfigureAwait(false);
        if (rV.Status != SequenceStatus.Succeeded) return rV;
        var rW = await cmdW.ConfigureAwait(false);
        if (rW.Status != SequenceStatus.Succeeded) return rW;

        // join: wait all three in-position
        var waitU = mgr.RunAsync(TcdSequenceKeys.AxisU_Wait_Zero, context, AxisWaitTimeout, cancellationToken);
        var waitV = mgr.RunAsync(TcdSequenceKeys.AxisV_Wait_Zero, context, AxisWaitTimeout, cancellationToken);
        var waitW = mgr.RunAsync(TcdSequenceKeys.AxisW_Wait_Zero, context, AxisWaitTimeout, cancellationToken);

        var wU = await waitU.ConfigureAwait(false);
        if (wU.Status != SequenceStatus.Succeeded) return wU;
        var wV = await waitV.ConfigureAwait(false);
        if (wV.Status != SequenceStatus.Succeeded) return wV;
        var wW = await waitW.ConfigureAwait(false);
        if (wW.Status != SequenceStatus.Succeeded) return wW;

        return SequenceResult.Success();
    }

    #endregion
}
