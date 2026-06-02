using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Sequences;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본딩 (Z 상승 → 대기 → Z 하강 → 본드 제품 생성).</summary>
public sealed class SemiAutoBondSequence : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;

    private static readonly TimeSpan ZWaitTimeout = TimeSpan.FromSeconds(3);
    #endregion

    public SemiAutoBondSequence(SequenceManager mgr) => _mgr = mgr;

    public override string Key         => TcdSequenceKeys.SEMI_Bond;
    public override string DisplayName => "SEMI: Bond";

    protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
    {
        await RunAsync(TcdSequenceKeys.AxisZ_Command_Bond, context, ct);
        await RunAsync(TcdSequenceKeys.AxisZ_Wait_Bond, context, ZWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Delay_Bond_Dwell1s, context, ct);
        await RunAsync(TcdSequenceKeys.AxisZ_Command_Load, context, ct);
        await RunAsync(TcdSequenceKeys.AxisZ_Wait_Load, context, ZWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Material_Create_Bonded, context, ct);
    }

    private async Task RunAsync(string key, ISequenceContext ctx, CancellationToken ct)
    {
        var result = await _mgr.RunAsync(key, ctx, null, ct).ConfigureAwait(false);
        if (result.Status == SequenceStatus.Stopped)
            throw new OperationCanceledException(ct);
        if (result.Status != SequenceStatus.Succeeded)
            throw new InvalidOperationException($"'{key}' failed");
    }

    private async Task RunAsync(string key, ISequenceContext ctx, TimeSpan timeout, CancellationToken ct)
    {
        var result = await _mgr.RunAsync(key, ctx, timeout, ct).ConfigureAwait(false);
        if (result.Status == SequenceStatus.Stopped)
            throw new OperationCanceledException(ct);
        if (result.Status != SequenceStatus.Succeeded)
            throw new InvalidOperationException($"'{key}' timeout");
    }
}
