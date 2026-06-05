using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.App.Sequences;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>
/// SEMI: 하부 챔버 Pick → 하부 스테이지(Stage2) Place (언로드).
/// 인터락: 하부 챔버 자재 있음, Robot @ Home.
/// </summary>
public sealed class SemiAutoLowerChamberPick_LowerStagePlace : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;
    private readonly TcdSimulation   _sim;

    private static readonly TimeSpan RobotTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BlowTime      = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan GripBlowTime  = TimeSpan.FromMilliseconds(200);
    #endregion

    public SemiAutoLowerChamberPick_LowerStagePlace(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public override string Key         => TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace;
    public override string DisplayName => "SEMI: Lower Chamber Pick → Lower Stage Place";

    protected override async Task DeviceActionAsync(ISequenceContext ctx, object parameter, CancellationToken ct)
    {
        // ── 인터락 ─────────────────────────────────────────────────────────────
        if (_sim.Materials.Get(MaterialLocation.LowerChamber) == null)
            throw new InvalidOperationException(AlarmKeys.ChamberNotEmpty);
        if (_sim.Robot.CurrentPosition != RobotPosition.Home)
            throw new InvalidOperationException(AlarmKeys.RobotNotAtHome);

        // ── Z Ready 인터락 ─────────────────────────────────────────────────────
        var lowerReady = await _sim.Plc.ReadBitAsync(DiBit.LowerChamberAtReady, ct).ConfigureAwait(false);
        var upperReady = await _sim.Plc.ReadBitAsync(DiBit.UpperChamberAtReady, ct).ConfigureAwait(false);
        if (!lowerReady || !upperReady)
            throw new InvalidOperationException(AlarmKeys.ZNotAtReadyForRobotEntry);

        // ── Pick: Lower Chamber ───────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberContact, ctx, RobotTimeout, ct);

        await RunAsync(TcdSequenceKeys.Manual_Io_LowerEscDisable, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowerChamberVacOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowerChamberBlowOn, ctx, ct);
        await ctx.Time.Delay(BlowTime, ct).ConfigureAwait(false);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowerChamberBlowOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripVacOn, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Pick_LowerChamber, ctx, ct);

        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);

        // ── Place: Lower Stage ────────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageContact, ctx, RobotTimeout, ct);

        await RunAsync(TcdSequenceKeys.Manual_Io_LowStageVacOn, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripVacOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripBlowOn, ctx, ct);
        await ctx.Time.Delay(GripBlowTime, ct).ConfigureAwait(false);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripBlowOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Place_Stage2, ctx, ct);

        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);
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
