using System;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

/// <summary>
/// IO 수동 시퀀스 팩토리.
/// 챔버 진공 / ESC 계열은 인터락 포함, 나머지는 단순 On/Off.
/// </summary>
public static class Manual_Io
{
  public static void RegisterAll(SequenceManager mgr)
  {
    mgr.Register(LowStageVacOn());
    mgr.Register(LowStageVacOff());
    mgr.Register(HighStageVacOn());
    mgr.Register(HighStageVacOff());
    mgr.Register(RobotGripVacOn());
    mgr.Register(RobotGripVacOff());
    mgr.Register(LowerChamberVacOn());
    mgr.Register(LowerChamberVacOff());
    mgr.Register(UpperChamberVacOn());
    mgr.Register(UpperChamberVacOff());
    mgr.Register(UpperEscEnable());
    mgr.Register(UpperEscDisable());
    mgr.Register(LowerEscEnable());
    mgr.Register(LowerEscDisable());
  }

  // ── Stage / Robot Vac (인터락 없음) ────────────────────────────────────

  public static ISequence LowStageVacOn() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowStageVacOn, "Lower Stage Vac On",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.LowStageVacOn, true, ct).ConfigureAwait(false);
    });

  public static ISequence LowStageVacOff() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowStageVacOff, "Lower Stage Vac Off",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.LowStageVacOn, false, ct).ConfigureAwait(false);
    });

  public static ISequence HighStageVacOn() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_HighStageVacOn, "Upper Stage Vac On",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.HighStageVacOn, true, ct).ConfigureAwait(false);
    });

  public static ISequence HighStageVacOff() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_HighStageVacOff, "Upper Stage Vac Off",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.HighStageVacOn, false, ct).ConfigureAwait(false);
    });

  public static ISequence RobotGripVacOn() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_RobotGripVacOn, "Robot Grip Vac On",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.RobotGripVacOn, true, ct).ConfigureAwait(false);
    });

  public static ISequence RobotGripVacOff() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_RobotGripVacOff, "Robot Grip Vac Off",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.RobotGripVacOn, false, ct).ConfigureAwait(false);
    });

  // ── Chamber Vac (인터락: LowerZ && UpperZ @ Bond 위치) ─────────────────

  public static ISequence LowerChamberVacOn() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowerChamberVacOn, "Lower Chamber Vac On",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await CheckChamberClosedAsync(plc, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.LowerChamberVacOn, true, ct).ConfigureAwait(false);
    });

  public static ISequence LowerChamberVacOff() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowerChamberVacOff, "Lower Chamber Vac Off",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.LowerChamberVacOn, false, ct).ConfigureAwait(false);
    });

  public static ISequence UpperChamberVacOn() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_UpperChamberVacOn, "Upper Chamber Vac On",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await CheckChamberClosedAsync(plc, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.UpperChamberVacOn, true, ct).ConfigureAwait(false);
    });

  public static ISequence UpperChamberVacOff() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_UpperChamberVacOff, "Upper Chamber Vac Off",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.UpperChamberVacOn, false, ct).ConfigureAwait(false);
    });

  // ── ESC (인터락: 해당 챔버 진공 On 확인) ───────────────────────────────

  public static ISequence UpperEscEnable() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_UpperEscEnable, "Upper ESC Enable",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      var vacOn = await plc.ReadBitAsync(DiBit.UpperChamberVac, ct).ConfigureAwait(false);
      if (!vacOn)
        throw new InvalidOperationException(AlarmKeys.UpperChamberVacNotReady);
      await plc.WriteBitAsync(DoBit.UpperEscEnable, true, ct).ConfigureAwait(false);
    });

  public static ISequence UpperEscDisable() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_UpperEscDisable, "Upper ESC Disable",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.UpperEscEnable, false, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.UpperEscDisable, true, ct).ConfigureAwait(false);
      await Task.Delay(200, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.UpperEscDisable, false, ct).ConfigureAwait(false);
    });

  public static ISequence LowerEscEnable() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowerEscEnable, "Lower ESC Enable",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      var vacOn = await plc.ReadBitAsync(DiBit.LowerChamberVac, ct).ConfigureAwait(false);
      if (!vacOn)
        throw new InvalidOperationException(AlarmKeys.LowerChamberVacNotReady);
      await plc.WriteBitAsync(DoBit.LowerEscEnable, true, ct).ConfigureAwait(false);
    });

  public static ISequence LowerEscDisable() => new DelegateSequence(
    TcdSequenceKeys.Manual_Io_LowerEscDisable, "Lower ESC Disable",
    async (ctx, p, ct) =>
    {
      var plc = MainCore.Instance.Simulation.Plc;
      await plc.WriteBitAsync(DoBit.LowerEscEnable, false, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.LowerEscDisable, true, ct).ConfigureAwait(false);
      await Task.Delay(200, ct).ConfigureAwait(false);
      await plc.WriteBitAsync(DoBit.LowerEscDisable, false, ct).ConfigureAwait(false);
    });

  // ── 공통 인터락 헬퍼 ──────────────────────────────────────────────────

  /// <summary>
  /// 챔버 진공 On 인터락.
  /// LowerChamberAtBond AND UpperChamberAtBond 센서가 모두 true여야 함.
  /// </summary>
  private static async Task CheckChamberClosedAsync(IPlc plc, System.Threading.CancellationToken ct)
  {
    var lowerAtBond = await plc.ReadBitAsync(DiBit.LowerChamberAtBond, ct)
      .ConfigureAwait(false);
    var upperAtBond = await plc.ReadBitAsync(DiBit.UpperChamberAtBond, ct)
      .ConfigureAwait(false);

    if (!lowerAtBond || !upperAtBond)
      throw new InvalidOperationException(AlarmKeys.ChamberNotClosed);
  }
}
