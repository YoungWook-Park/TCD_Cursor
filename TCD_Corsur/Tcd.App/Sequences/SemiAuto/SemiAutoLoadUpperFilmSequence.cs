using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 상부 필름을 상부 챔버로 로드.</summary>
public sealed class SemiAutoLoadUpperFilmSequence : ISequence
{
    #region Variables

    public string Key => TcdSequenceKeys.SEMI_LoadUpperFilm;
    public string DisplayName => "SEMI: Load upper film to upper chamber";
    public object Param { get; set; } = null!;

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        Param = parameter;
        var core = MainCore.Instance;
        core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid() };

        var sim = core.Simulation;
        var mgr = core.Sequences;

        if (sim.Materials.Get(MaterialLocation.UpperChamber) != null)
        {
            context.Alarms.Raise(new Alarm("CHAMBER_NOT_EMPTY", "Upper chamber is not empty.", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail("Upper chamber is not empty.");
        }

        var result = await mgr.RunAsync(TcdSequenceKeys.Robot_Move_Stage, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Wait_Stage, context, RobotWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Pick_Stage1, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Move_UpperLoad, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Wait_UpperLoad, context, RobotWaitTimeout, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        result = await mgr.RunAsync(TcdSequenceKeys.Robot_Place_UpperChamber, context, null, cancellationToken).ConfigureAwait(false);
        if (result.Status != SequenceStatus.Succeeded) return result;

        return SequenceResult.Success();
    }

    #endregion
}
