using Tcd.Devices;

namespace Tcd.App.Define;

/// <summary>
/// 로봇 포지션 UI 표시 이름 상수.
/// ExecuteMove / 레시피 키 / 로그 출력 등에서 리터럴 문자열 대신 사용.
/// </summary>
public static class RobotPositionName
{
  public const string Home                    = "Home";
  public const string Ready                   = "Ready";
  public const string S1_PickupWait           = "S1 PickupWait";
  public const string S1_Pick                 = "S1 Pick";
  public const string S2_PickupWait           = "S2 PickupWait";
  public const string S2_Pick                 = "S2 Pick";
  public const string UpperChamber_PickupWait = "UC PickupWait";
  public const string UpperChamber_Pick       = "UC Pick";
  public const string LowerChamber_PickupWait = "LC PickupWait";
  public const string LowerChamber_Pick       = "LC Pick";
  public const string Peel                    = "Peel";

  /// <summary>레시피 dict 키 조회용 — RobotPosition → 표시명 매핑.</summary>
  public static string FromPosition(RobotPosition pos) => pos switch
  {
    RobotPosition.Home                    => Home,
    RobotPosition.Ready                   => Ready,
    RobotPosition.S1_PickupWait           => S1_PickupWait,
    RobotPosition.S1_Pick                 => S1_Pick,
    RobotPosition.S2_PickupWait           => S2_PickupWait,
    RobotPosition.S2_Pick                 => S2_Pick,
    RobotPosition.UpperChamber_PickupWait => UpperChamber_PickupWait,
    RobotPosition.UpperChamber_Pick       => UpperChamber_Pick,
    RobotPosition.LowerChamber_PickupWait => LowerChamber_PickupWait,
    RobotPosition.LowerChamber_Pick       => LowerChamber_Pick,
    RobotPosition.Peel                    => Peel,
    _                                     => pos.ToString(),
  };
}

/// <summary>
/// 포지션별 기본 이동 속도 (0-100 %).
/// 레시피에 값이 없을 경우 폴백으로만 사용.
/// 실제 속도는 TcdRecipe.RobotVelocity 에서 읽어야 한다.
/// </summary>
public static class RobotVelocityDefault
{
  public const int Home                    = 30;
  public const int Ready                   = 50;
  public const int S1_PickupWait           = 60;
  public const int S1_Pick                 = 30;
  public const int S2_PickupWait           = 60;
  public const int S2_Pick                 = 30;
  public const int UpperChamber_PickupWait = 60;
  public const int UpperChamber_Pick       = 30;
  public const int LowerChamber_PickupWait = 60;
  public const int LowerChamber_Pick       = 30;
  public const int Peel                    = 20;

  /// <summary>RobotPosition → 기본 속도 매핑 (폴백 전용).</summary>
  public static int ForPosition(RobotPosition pos) => pos switch
  {
    RobotPosition.Home                    => Home,
    RobotPosition.Ready                   => Ready,
    RobotPosition.S1_PickupWait           => S1_PickupWait,
    RobotPosition.S1_Pick                 => S1_Pick,
    RobotPosition.S2_PickupWait           => S2_PickupWait,
    RobotPosition.S2_Pick                 => S2_Pick,
    RobotPosition.UpperChamber_PickupWait => UpperChamber_PickupWait,
    RobotPosition.UpperChamber_Pick       => UpperChamber_Pick,
    RobotPosition.LowerChamber_PickupWait => LowerChamber_PickupWait,
    RobotPosition.LowerChamber_Pick       => LowerChamber_Pick,
    RobotPosition.Peel                    => Peel,
    _                                     => 50,
  };
}
