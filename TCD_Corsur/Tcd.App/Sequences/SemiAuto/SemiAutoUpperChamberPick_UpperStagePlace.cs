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
/// SEMI: 상부 챔버 Pick → 상부 스테이지(Stage1) Place (언로드).
/// 인터락: 상부 챔버 자재 있음, Robot @ Home.
/// </summary>
public sealed class SemiAutoUpperChamberPick_UpperStagePlace : SequenceBase
{
    #region Variables
    private readonly SequenceManager _mgr;
    private readonly TcdSimulation   _sim;

    private static readonly TimeSpan RobotTimeout  = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BlowTime       = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan GripBlowTime   = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan EscDisableTime = TimeSpan.FromMilliseconds(200);
    #endregion

    public SemiAutoUpperChamberPick_UpperStagePlace(SequenceManager mgr, TcdSimulation sim)
    {
        _mgr = mgr;
        _sim = sim;
    }

    public override string Key         => TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace;
    public override string DisplayName => "SEMI: Upper Chamber Pick → Upper Stage Place";

    protected override async Task DeviceActionAsync(ISequenceContext ctx, object parameter, CancellationToken ct)
    {
        // ── 인터락 ─────────────────────────────────────────────────────────────
        if (_sim.Materials.Get(MaterialLocation.UpperChamber) == null)
            throw new InvalidOperationException(AlarmKeys.ChamberNotEmpty);
        if (_sim.Robot.CurrentPosition != RobotPosition.Home)
            throw new InvalidOperationException(AlarmKeys.RobotNotAtHome);

        // ── Z Ready 인터락 ─────────────────────────────────────────────────────
        var lowerReady = await _sim.Plc.ReadBitAsync(DiBit.LowerChamberAtReady, ct).ConfigureAwait(false);
        var upperReady = await _sim.Plc.ReadBitAsync(DiBit.UpperChamberAtReady, ct).ConfigureAwait(false);
        if (!lowerReady || !upperReady)
            throw new InvalidOperationException(AlarmKeys.ZNotAtReadyForRobotEntry);

        // ── Pick: Upper Chamber ───────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_UpperChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperChamberWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_UpperChamberContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperChamberContact, ctx, RobotTimeout, ct);

        await _sim.Plc.WriteBitAsync(DoBit.UpperEscEnable,    false, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.UpperEscDisable,   true,  ct).ConfigureAwait(false);
        await _sim.Time.Delay(EscDisableTime, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.UpperEscDisable,   false, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.UpperChamberVacOn, false, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.UpperChamberBlow,  true,  ct).ConfigureAwait(false);
        await _sim.Time.Delay(BlowTime, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.UpperChamberBlow,  false, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.RobotGripVacOn,    true,  ct).ConfigureAwait(false);
        await _sim.Robot.PickAsync(MaterialLocation.UpperChamber, ct).ConfigureAwait(false);

        await RunAsync(TcdSequenceKeys.Robot_Move_UpperChamberWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperChamberWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);

        // ── Place: Upper Stage ────────────────────────────────────────────────
        await RunAsync(TcdSequenceKeys.Robot_Move_UpperStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperStageWait, ctx, RobotTimeout, ct);
        await RunAsync(TcdSequenceKeys.Robot_Move_UpperStageContact, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperStageContact, ctx, RobotTimeout, ct);

        await _sim.Plc.WriteBitAsync(DoBit.HighStageVacOn,  true,  ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.RobotGripVacOn,  false, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.RobotGripBlow,   true,  ct).ConfigureAwait(false);
        await _sim.Time.Delay(GripBlowTime, ct).ConfigureAwait(false);
        await _sim.Plc.WriteBitAsync(DoBit.RobotGripBlow,   false, ct).ConfigureAwait(false);
        await _sim.Robot.PlaceAsync(MaterialLocation.Stage1, ct).ConfigureAwait(false);

        await RunAsync(TcdSequenceKeys.Robot_Move_UpperStageWait, ctx, ct);
        await RunAsync(TcdSequenceKeys.Robot_Wait_UpperStageWait, ctx, RobotTimeout, ct);
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
