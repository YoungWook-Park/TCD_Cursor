using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본딩 (Z 상승 → 대기 → Z 하강 → 본드 제품 생성).</summary>
public sealed class SemiAutoBondSequence : ISequence
{
    #region Variables

    public string Key => TcdSequenceKeys.SEMI_Bond;
    public string DisplayName => "SEMI: Bond";
    public object Param { get; set; } = null!;

    private static readonly TimeSpan ZWaitTimeout = TimeSpan.FromSeconds(3);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid() };

        var mgr = core.Sequences;

        var result = await mgr.RunAsync(TcdSequenceKeys.AxisZ_Command_Bond, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.AxisZ_Wait_Bond, context, ZWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Delay_Bond_Dwell1s, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.AxisZ_Command_Load, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.AxisZ_Wait_Load, context, ZWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Material_Create_Bonded, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        return SequenceResult.Success();
    }

    #endregion
}
