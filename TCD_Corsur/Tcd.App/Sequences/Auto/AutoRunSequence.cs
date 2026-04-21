using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Auto;

/// <summary>AUTO: 스테이지 로드 대기 → 상부/하부 로드 → 로봇 홈 → UVW 정렬 → 본딩 → 스테이지2 언로드.</summary>
public sealed class AutoRunSequence : ISequence
{
    #region Variables

    public string Key => TcdSequenceKeys.AUTO_Run;
    public string DisplayName => "AUTO: Stage -> Load -> Align -> Bond -> Unload";
    public object Param { get; set; } = null!;

    private static readonly TimeSpan PlcStageLoadTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RobotWaitHomeTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid() };

        var mgr = core.Sequences;

        var result = await mgr.RunAsync(TcdSequenceKeys.Plc_Wait_StageLoaded, context, PlcStageLoadTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.SEMI_LoadUpperFilm, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.SEMI_LoadLowerFilm, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Move_Home, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Wait_Home, context, RobotWaitHomeTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.SEMI_AlignUVW, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.SEMI_Bond, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.SEMI_UnloadProductToStage2, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        return SequenceResult.Success();
    }

    #endregion
}
