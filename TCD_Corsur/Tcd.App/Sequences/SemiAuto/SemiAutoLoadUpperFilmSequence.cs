using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.Core;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 상부 필름을 상부 챔버로 로드.</summary>
public sealed class SemiAutoLoadUpperFilmSequence : ISequence
{
    #region Variables

    private readonly SequenceManager _mgr;
    private readonly TcdSimulation _sim;

    public SemiAutoLoadUpperFilmSequence(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public string Key => TcdSequenceKeys.SEMI_LoadUpperFilm;
    public string DisplayName => "SEMI: Load upper film to upper chamber";

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);

    #endregion

    #region Function

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        var sim = _sim;
        var mgr = _mgr;

        if (sim.Materials.Get(MaterialLocation.UpperChamber) != null)
        {
            context.Alarms.Raise(new Alarm(AlarmKeys.ChamberNotEmpty, "Upper chamber is not empty.", AlarmSeverity.Error, context.Time.Now));
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
