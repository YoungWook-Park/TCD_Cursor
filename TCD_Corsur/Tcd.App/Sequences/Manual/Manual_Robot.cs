using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

/// <summary>
/// 로봇 수동 이동 시퀀스 팩토리.
/// 챔버 진입 포지션은 Z Ready 인터락 포함.
/// 공통 플로우(SetVelocity → Move → WaitForPosition)는 ExecuteMoveAsync로 재사용.
/// </summary>
public static class Manual_Robot
{
  public static void RegisterAll(SequenceManager mgr)
  {
    mgr.Register(MoveHome());
    mgr.Register(MoveUpperStageWait());
    mgr.Register(MoveUpperStageContact());
    mgr.Register(MoveLowerStageWait());
    mgr.Register(MoveLowerStageContact());
    mgr.Register(MoveUpperChamberWait());
    mgr.Register(MoveUpperChamberContact());
    mgr.Register(MoveLowerChamberWait());
    mgr.Register(MoveLowerChamberContact());
  }

  // ── 스테이지 / Home (인터락 없음) ─────────────────────────────────────

  public static ISequence MoveHome() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_Home, "Robot Move Home",
    async (ctx, p, ct) =>
      await ExecuteMoveAsync(RobotPosition.Home, p, ct).ConfigureAwait(false));

  public static ISequence MoveUpperStageWait() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_UpperStageWait, "Robot Move Upper Stage Wait",
    async (ctx, p, ct) =>
      await ExecuteMoveAsync(RobotPosition.UpperStageWait, p, ct).ConfigureAwait(false));

  public static ISequence MoveUpperStageContact() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_UpperStageContact, "Robot Move Upper Stage Contact",
    async (ctx, p, ct) =>
      await ExecuteMoveAsync(RobotPosition.UpperStageContact, p, ct).ConfigureAwait(false));

  public static ISequence MoveLowerStageWait() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_LowerStageWait, "Robot Move Lower Stage Wait",
    async (ctx, p, ct) =>
      await ExecuteMoveAsync(RobotPosition.LowerStageWait, p, ct).ConfigureAwait(false));

  public static ISequence MoveLowerStageContact() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_LowerStageContact, "Robot Move Lower Stage Contact",
    async (ctx, p, ct) =>
      await ExecuteMoveAsync(RobotPosition.LowerStageContact, p, ct).ConfigureAwait(false));

  // ── 챔버 진입 (인터락: LowerChamberAtReady && UpperChamberAtReady) ───────

  public static ISequence MoveUpperChamberWait() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_UpperChamberWait, "Robot Move Upper Chamber Wait",
    async (ctx, p, ct) =>
    {
      await CheckZReadyAsync(ct).ConfigureAwait(false);
      await ExecuteMoveAsync(RobotPosition.UpperChamberWait, p, ct).ConfigureAwait(false);
    });

  public static ISequence MoveUpperChamberContact() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_UpperChamberContact, "Robot Move Upper Chamber Contact",
    async (ctx, p, ct) =>
    {
      await CheckZReadyAsync(ct).ConfigureAwait(false);
      await ExecuteMoveAsync(RobotPosition.UpperChamberContact, p, ct).ConfigureAwait(false);
    });

  public static ISequence MoveLowerChamberWait() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_LowerChamberWait, "Robot Move Lower Chamber Wait",
    async (ctx, p, ct) =>
    {
      await CheckZReadyAsync(ct).ConfigureAwait(false);
      await ExecuteMoveAsync(RobotPosition.LowerChamberWait, p, ct).ConfigureAwait(false);
    });

  public static ISequence MoveLowerChamberContact() => new DelegateSequence(
    TcdSequenceKeys.Manual_Robot_LowerChamberContact, "Robot Move Lower Chamber Contact",
    async (ctx, p, ct) =>
    {
      await CheckZReadyAsync(ct).ConfigureAwait(false);
      await ExecuteMoveAsync(RobotPosition.LowerChamberContact, p, ct).ConfigureAwait(false);
    });

  // ── 공통 플로우 ───────────────────────────────────────────────────────

  /// <summary>SetVelocity → Move → WaitForPosition 공통 흐름.</summary>
  private static async Task ExecuteMoveAsync(
    RobotPosition position, object? param, CancellationToken ct)
  {
    var core    = MainCore.Instance;
    var robot   = core.RobotDevice;
    var timeout = core.Settings.RobotMoveTimeout;
    var velocity = param is int v ? v : RobotVelocityDefault.ForPosition(position);

    await robot.SetVelocityAsync(position, velocity, ct).ConfigureAwait(false);
    var ok = await robot.MoveAsync(position, ct).ConfigureAwait(false);
    if (!ok)
      throw new InvalidOperationException(
        $"Robot rejected move to {RobotPositionName.FromPosition(position)}");
    await robot.WaitForPositionAsync(position, timeout, ct).ConfigureAwait(false);
  }

  /// <summary>
  /// Z Ready 인터락.
  /// LowerChamberAtReady AND UpperChamberAtReady 센서가 모두 true여야 함.
  /// </summary>
  private static async Task CheckZReadyAsync(CancellationToken ct)
  {
    var plc = MainCore.Instance.Simulation.Plc;
    var lowerReady = await plc.ReadBitAsync(
      DiBit.LowerChamberAtReady, ct).ConfigureAwait(false);
    var upperReady = await plc.ReadBitAsync(
      DiBit.UpperChamberAtReady, ct).ConfigureAwait(false);

    if (!lowerReady || !upperReady)
      throw new InvalidOperationException(AlarmKeys.ZNotAtReadyForRobotEntry);
  }
}
