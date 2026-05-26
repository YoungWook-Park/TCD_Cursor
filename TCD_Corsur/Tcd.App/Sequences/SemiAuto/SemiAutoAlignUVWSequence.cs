using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: UVW 정렬. 인터락: 로봇이 홈 위치에 있어야 함. U/V/W 동시 명령 후 대기.</summary>
public sealed class SemiAutoAlignUVWSequence : ISequence
{
    #region Variables

    private readonly SequenceManager _mgr;
    private readonly TcdSimulation _sim;

    public SemiAutoAlignUVWSequence(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public string Key => TcdSequenceKeys.SEMI_AlignUVW;
    public string DisplayName => "SEMI: Align UVW";

    private static readonly TimeSpan AxisWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        if (_sim.Robot.CurrentPosition != RobotPosition.Home)
        {
            context.Alarms.Raise(new Alarm(AlarmKeys.RobotNotAtHome, "UVW align interlock: Robot must be at home position.", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail("Robot must be at home before UVW align.");
        }

        // fork: command U, V, W simultaneously
        var cmdResults = await Task.WhenAll(
            _mgr.RunAsync(TcdSequenceKeys.AxisU_Command_Zero, context, null, cancellationToken),
            _mgr.RunAsync(TcdSequenceKeys.AxisV_Command_Zero, context, null, cancellationToken),
            _mgr.RunAsync(TcdSequenceKeys.AxisW_Command_Zero, context, null, cancellationToken)
        ).ConfigureAwait(false);

        var cmdFail = cmdResults.FirstOrDefault(r => r.Status != SequenceStatus.Succeeded);
        if (cmdFail != null) return cmdFail;

        // join: wait all three in-position
        var waitResults = await Task.WhenAll(
            _mgr.RunAsync(TcdSequenceKeys.AxisU_Wait_Zero, context, AxisWaitTimeout, cancellationToken),
            _mgr.RunAsync(TcdSequenceKeys.AxisV_Wait_Zero, context, AxisWaitTimeout, cancellationToken),
            _mgr.RunAsync(TcdSequenceKeys.AxisW_Wait_Zero, context, AxisWaitTimeout, cancellationToken)
        ).ConfigureAwait(false);

        var waitFail = waitResults.FirstOrDefault(r => r.Status != SequenceStatus.Succeeded);
        if (waitFail != null) return waitFail;

        return SequenceResult.Success();
    }

    #endregion
}
