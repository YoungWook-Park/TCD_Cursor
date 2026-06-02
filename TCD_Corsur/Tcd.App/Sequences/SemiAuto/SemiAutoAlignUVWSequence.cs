using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.App.Sequences;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: UVW 정렬. 인터락: 로봇이 홈 위치에 있어야 함. U/V/W 동시 명령 후 대기.</summary>
public sealed class SemiAutoAlignUVWSequence : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;
    private readonly TcdSimulation   _sim;

    private static readonly System.TimeSpan AxisWaitTimeout = System.TimeSpan.FromSeconds(2);
    #endregion

    public SemiAutoAlignUVWSequence(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public override string Key         => TcdSequenceKeys.SEMI_AlignUVW;
    public override string DisplayName => "SEMI: Align UVW";

    protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
    {
        if (_sim.Robot.CurrentPosition != RobotPosition.Home)
            throw new System.InvalidOperationException(AlarmKeys.RobotNotAtHome);

        var cmdResults = await Task.WhenAll(
            _mgr.RunAsync(TcdSequenceKeys.AxisU_Command_Zero, context, null, ct),
            _mgr.RunAsync(TcdSequenceKeys.AxisV_Command_Zero, context, null, ct),
            _mgr.RunAsync(TcdSequenceKeys.AxisW_Command_Zero, context, null, ct)
        ).ConfigureAwait(false);

        if (cmdResults.Any(r => r.Status == SequenceStatus.Stopped))
            throw new System.OperationCanceledException(ct);
        if (cmdResults.Any(r => r.Status != SequenceStatus.Succeeded))
            throw new System.InvalidOperationException("UVW command failed");

        var waitResults = await Task.WhenAll(
            _mgr.RunAsync(TcdSequenceKeys.AxisU_Wait_Zero, context, AxisWaitTimeout, ct),
            _mgr.RunAsync(TcdSequenceKeys.AxisV_Wait_Zero, context, AxisWaitTimeout, ct),
            _mgr.RunAsync(TcdSequenceKeys.AxisW_Wait_Zero, context, AxisWaitTimeout, ct)
        ).ConfigureAwait(false);

        if (waitResults.Any(r => r.Status == SequenceStatus.Stopped))
            throw new System.OperationCanceledException(ct);
        if (waitResults.Any(r => r.Status != SequenceStatus.Succeeded))
            throw new System.InvalidOperationException("UVW wait timeout");
    }
}
