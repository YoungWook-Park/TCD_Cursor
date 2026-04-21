using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본드 제품을 하부 챔버에서 스테이지2로 언로드.</summary>
public sealed class SemiAutoUnloadProductToStage2Sequence : ISequence
{
    #region Variables

    public string Key => TcdSequenceKeys.SEMI_UnloadProductToStage2;
    public string DisplayName => "SEMI: Unload product to stage2";
    public object Param { get; set; } = null!;

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid() };

        var mgr = core.Sequences;

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
