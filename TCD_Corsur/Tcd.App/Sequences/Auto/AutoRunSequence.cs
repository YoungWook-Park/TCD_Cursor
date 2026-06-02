using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Sequences;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Auto;

/// <summary>AUTO: 스테이지 로드 대기 → 상부/하부 로드 → 로봇 홈 → UVW 정렬 → 본딩 → 스테이지2 언로드.</summary>
public sealed class AutoRunSequence : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;

    private static readonly TimeSpan PlcStageLoadTimeout  = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RobotWaitHomeTimeout = TimeSpan.FromSeconds(2);
    #endregion

    public AutoRunSequence(SequenceManager mgr) => _mgr = mgr;

    public override string Key         => TcdSequenceKeys.AUTO_Run;
    public override string DisplayName => "AUTO: Stage -> Load -> Align -> Bond -> Unload";

    protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
    {
        await RunAsync(TcdSequenceKeys.Plc_Wait_StageLoaded, context, PlcStageLoadTimeout, ct);
        await RunAsync(TcdSequenceKeys.SEMI_LoadUpperFilm, context, ct);
        await RunAsync(TcdSequenceKeys.SEMI_LoadLowerFilm, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Home, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Home, context, RobotWaitHomeTimeout, ct);
        await RunAsync(TcdSequenceKeys.SEMI_AlignUVW, context, ct);
        await RunAsync(TcdSequenceKeys.SEMI_Bond, context, ct);
        await RunAsync(TcdSequenceKeys.SEMI_UnloadProductToStage2, context, ct);
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
