using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Sequences;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 본드 제품을 하부 챔버에서 스테이지2로 언로드.</summary>
public sealed class SemiAutoUnloadProductToStage2Sequence : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);
    #endregion

    public SemiAutoUnloadProductToStage2Sequence(SequenceManager mgr) => _mgr = mgr;

    public override string Key         => TcdSequenceKeys.SEMI_UnloadProductToStage2;
    public override string DisplayName => "SEMI: Unload product to stage2";

    protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
    {
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerLoad, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerLoad, context, RobotWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Pick_LowerChamber, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Stage, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Stage, context, RobotWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Place_Stage2, context, ct);
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
