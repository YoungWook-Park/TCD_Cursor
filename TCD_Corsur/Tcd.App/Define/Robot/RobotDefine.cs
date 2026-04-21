namespace Tcd.App.Define;

/// <summary>
/// 로봇 포지션 UI 표시 이름 상수.
/// ExecuteMove / 로그 출력 등에서 리터럴 문자열 대신 사용.
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
}

/// <summary>
/// 로봇 포지션별 기본 이동 속도 (0-100 %).
/// SetVelocityAsync 호출 전 RobotVelocity 상수로 통일.
/// </summary>
public static class RobotVelocity
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
}
