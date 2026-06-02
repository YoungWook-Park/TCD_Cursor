using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.App.Sequences;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>SEMI: 하부 필름을 하부 챔버로 로드.</summary>
public sealed class SemiAutoLoadLowerFilmSequence : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;
    private readonly TcdSimulation   _sim;

    private static readonly TimeSpan RobotWaitTimeout = TimeSpan.FromSeconds(2);
    #endregion

    public SemiAutoLoadLowerFilmSequence(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public override string Key         => TcdSequenceKeys.SEMI_LoadLowerFilm;
    public override string DisplayName => "SEMI: Load lower film to lower chamber";

    protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
    {
        if (_sim.Materials.Get(MaterialLocation.LowerChamber) != null)
            throw new InvalidOperationException(AlarmKeys.ChamberNotEmpty);

        await RunAsync(TcdSequenceKeys.Robot_Move_Stage, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Stage, context, RobotWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Pick_Stage2, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerLoad, context, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerLoad, context, RobotWaitTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Place_LowerChamber, context, ct);
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
