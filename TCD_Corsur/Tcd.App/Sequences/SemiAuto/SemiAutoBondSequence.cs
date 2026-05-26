using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본딩 (Z 상승 → 대기 → Z 하강 → 본드 제품 생성).</summary>
public sealed class SemiAutoBondSequence : ISequence
{
    #region Variables

    private readonly SequenceManager _mgr;

    public SemiAutoBondSequence(SequenceManager mgr) => _mgr = mgr;

    public string Key => TcdSequenceKeys.SEMI_Bond;
    public string DisplayName => "SEMI: Bond";

    private static readonly TimeSpan ZWaitTimeout = TimeSpan.FromSeconds(3);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        var mgr = _mgr;

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
