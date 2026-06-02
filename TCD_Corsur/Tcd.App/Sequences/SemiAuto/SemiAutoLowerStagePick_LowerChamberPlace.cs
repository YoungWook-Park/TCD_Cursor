using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.SemiAuto;

/// <summary>
/// SEMI: 하부 스테이지(Stage2) Pick → 하부 챔버 Place.
/// 인터락: 하부 챔버 비어있음, Robot @ Home.
/// </summary>
public sealed class SemiAutoLowerStagePick_LowerChamberPlace : ISequence
{
  private readonly SequenceManager _mgr;
  private readonly TcdSimulation   _sim;

  public SemiAutoLowerStagePick_LowerChamberPlace(
    SequenceManager mgr, TcdSimulation sim)
  {
    _mgr = mgr;
    _sim = sim;
  }

  public string Key         => TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace;
  public string DisplayName => "SEMI: Lower Stage Pick → Lower Chamber Place";

  private static readonly TimeSpan RobotTimeout = TimeSpan.FromSeconds(3);
  private static readonly TimeSpan VacBuildTime  = TimeSpan.FromMilliseconds(500);
  private static readonly TimeSpan BlowTime      = TimeSpan.FromMilliseconds(300);
  private static readonly TimeSpan GripBlowTime  = TimeSpan.FromMilliseconds(200);

  public async Task<SequenceResult> ExecuteAsync(
    ISequenceContext ctx, object parameter, CancellationToken ct)
  {
    // ── 인터락 ───────────────────────────────────────────────────────────
    if (_sim.Materials.Get(MaterialLocation.LowerChamber) != null)
    {
      ctx.Alarms.Raise(new Alarm(AlarmKeys.ChamberNotEmpty,
        "Lower chamber is not empty.", AlarmSeverity.Error, ctx.Time.Now));
      return SequenceResult.Fail("Lower chamber not empty.");
    }
    if (_sim.Robot.CurrentPosition != RobotPosition.Home)
    {
      ctx.Alarms.Raise(new Alarm(AlarmKeys.RobotNotAtHome,
        "Robot must be at home before semi-auto.", AlarmSeverity.Error, ctx.Time.Now));
      return SequenceResult.Fail("Robot not at home.");
    }

    // ── Pick: Lower Stage ─────────────────────────────────────────────
    var r = await Run(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Move_LowerStageContact, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerStageContact, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;

    await _sim.Plc.WriteBitAsync(DoBit.LowStageVacOn,   false, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.RobotGripVacOn,  true,  ct).ConfigureAwait(false);
    await _sim.Robot.PickAsync(MaterialLocation.Stage2, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.LowStageBlow,    true,  ct).ConfigureAwait(false);
    await _sim.Time.Delay(BlowTime, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.LowStageBlow,    false, ct).ConfigureAwait(false);

    r = await Run(TcdSequenceKeys.Robot_Move_LowerStageWait, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerStageWait, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;

    // ── Z Ready 인터락 ────────────────────────────────────────────────
    var lowerReady = await _sim.Plc.ReadBitAsync(DiBit.LowerChamberAtReady, ct)
      .ConfigureAwait(false);
    var upperReady = await _sim.Plc.ReadBitAsync(DiBit.UpperChamberAtReady, ct)
      .ConfigureAwait(false);
    if (!lowerReady || !upperReady)
    {
      ctx.Alarms.Raise(new Alarm(AlarmKeys.ZNotAtReadyForRobotEntry,
        "Z axes must be at ready position before chamber entry.",
        AlarmSeverity.Error, ctx.Time.Now));
      return SequenceResult.Fail("Z not at ready.");
    }

    // ── Place: Lower Chamber ──────────────────────────────────────────
    r = await Run(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Move_LowerChamberContact, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerChamberContact, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;

    await _sim.Plc.WriteBitAsync(DoBit.LowerEscEnable,    true, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.LowerChamberVacOn, true, ct).ConfigureAwait(false);
    await _sim.Time.Delay(VacBuildTime, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.RobotGripVacOn,    false, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.RobotGripBlow,     true,  ct).ConfigureAwait(false);
    await _sim.Time.Delay(GripBlowTime, ct).ConfigureAwait(false);
    await _sim.Plc.WriteBitAsync(DoBit.RobotGripBlow,     false, ct).ConfigureAwait(false);
    await _sim.Robot.PlaceAsync(MaterialLocation.LowerChamber, ct).ConfigureAwait(false);

    r = await Run(TcdSequenceKeys.Robot_Move_LowerChamberWait, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_LowerChamberWait, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Move_Home, ctx, ct);
    if (!r.IsOk) return r.Result;
    r = await Run(TcdSequenceKeys.Robot_Wait_Home, ctx, RobotTimeout, ct);
    if (!r.IsOk) return r.Result;

    return SequenceResult.Success();
  }

  private async Task<(bool IsOk, SequenceResult Result)> Run(
    string key, ISequenceContext ctx, CancellationToken ct)
  {
    var result = await _mgr.RunAsync(key, ctx, null, ct).ConfigureAwait(false);
    return (result.Status == SequenceStatus.Succeeded, result);
  }

  private async Task<(bool IsOk, SequenceResult Result)> Run(
    string key, ISequenceContext ctx, TimeSpan timeout, CancellationToken ct)
  {
    var result = await _mgr.RunAsync(key, ctx, timeout, ct).ConfigureAwait(false);
    return (result.Status == SequenceStatus.Succeeded, result);
  }
}
