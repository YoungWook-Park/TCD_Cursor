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
/// SEMI: 하부 스테이지(Stage2) Pick → 하부 챔버 Place.
/// 인터락: 하부 챔버 비어있음, Robot @ Home.
/// </summary>
public sealed class SemiAutoLowerStagePick_LowerChamberPlace : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;
    private readonly TcdSimulation   _sim;

    private static readonly TimeSpan RobotTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan VacBuildTime  = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan BlowTime      = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan GripBlowTime  = TimeSpan.FromMilliseconds(200);
    #endregion

    public SemiAutoLowerStagePick_LowerChamberPlace(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public override string Key         => TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace;
    public override string DisplayName => "SEMI: Lower Stage Pick → Lower Chamber Place";

    protected override async Task DeviceActionAsync(ISequenceContext ctx, object parameter, CancellationToken ct)
    {
        // ── 인터락 ─────────────────────────────────────────────────────────────
        if (_sim.Materials.Get(MaterialLocation.LowerChamber) != null)
            throw new InvalidOperationException(AlarmKeys.ChamberNotEmpty);
        if (_sim.Robot.CurrentPosition != RobotPosition.Home)
            throw new InvalidOperationException(AlarmKeys.RobotNotAtHome);

        // ── Pick: Lower Stage ─────────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageContact, ctx, RobotTimeout, ct);

        await RunAsync(TcdSequenceKeys.Manual_Io_LowStageVacOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripVacOn, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Pick_Stage2, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowStageBlowOn, ctx, ct);
        await ctx.Time.Delay(BlowTime, ct).ConfigureAwait(false);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowStageBlowOff, ctx, ct);

        await RunAsync(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);

        // ── Z Ready 인터락 ─────────────────────────────────────────────────────
        var lowerReady = await _sim.Plc.ReadBitAsync(DiBit.LowerChamberAtReady, ct).ConfigureAwait(false);
        var upperReady = await _sim.Plc.ReadBitAsync(DiBit.UpperChamberAtReady, ct).ConfigureAwait(false);
        if (!lowerReady || !upperReady)
            throw new InvalidOperationException(AlarmKeys.ZNotAtReadyForRobotEntry);

        // ── Place: Lower Chamber ──────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberContact, ctx, RobotTimeout, ct);

        await RunAsync(TcdSequenceKeys.Manual_Io_LowerEscEnable, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_LowerChamberVacOn, ctx, ct);
        await ctx.Time.Delay(VacBuildTime, ct).ConfigureAwait(false);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripVacOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripBlowOn, ctx, ct);
        await ctx.Time.Delay(GripBlowTime, ct).ConfigureAwait(false);
        await RunAsync(TcdSequenceKeys.Manual_Io_RobotGripBlowOff, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Place_LowerChamber, ctx, ct);

        await RunAsync(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
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
