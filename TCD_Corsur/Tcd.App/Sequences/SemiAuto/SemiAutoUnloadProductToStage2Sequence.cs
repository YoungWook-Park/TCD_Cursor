using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본드 제품을 하부 챔버에서 스테이지2로 언로드.</summary>
public sealed class SemiAutoUnloadProductToStage2Sequence : ISequence
{
    #region Variables

    private readonly SequenceManager _mgr;

    public SemiAutoUnloadProductToStage2Sequence(SequenceManager mgr) => _mgr = mgr;

    public string Key => TcdSequenceKeys.SEMI_UnloadProductToStage2;
    public string DisplayName => "SEMI: Unload product to stage2";

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        var mgr = _mgr;

        var result = await mgr.RunAsync(TcdSequenceKeys.Robot_Move_LowerLoad, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Wait_LowerLoad, context, RobotWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Pick_LowerChamber, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Move_Stage, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Wait_Stage, context, RobotWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Place_Stage2, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        return SequenceResult.Success();
    }

    #endregion
}
