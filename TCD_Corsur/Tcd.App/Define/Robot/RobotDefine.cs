using Tcd.Devices;

namespace Tcd.App.Define;

/// <summary>
/// 로봇 포지션 UI 표시 이름 상수.
/// ExecuteMove / 레시피 키 / 로그 출력 등에서 리터럴 문자열 대신 사용.
/// </summary>
public static class RobotPositionName
{
  public const string Home               = "Home";
  public const string Ready              = "Ready";
  public const string UpperStageWait     = "UpperStageWait";
  public const string UpperStageContact  = "UpperStageContact";
  public const string LowerStageWait     = "LowerStageWait";
  public const string LowerStageContact  = "LowerStageContact";
  public const string UpperChamberWait   = "UpperChamberWait";
  public const string UpperChamberContact= "UpperChamberContact";
  public const string LowerChamberWait   = "LowerChamberWait";
  public const string LowerChamberContact= "LowerChamberContact";
  public const string Peel               = "Peel";

  public static string FromPosition(RobotPosition pos) => pos switch
  {
    RobotPosition.Home                => Home,
    RobotPosition.Ready               => Ready,
    RobotPosition.UpperStageWait      => UpperStageWait,
    RobotPosition.UpperStageContact   => UpperStageContact,
    RobotPosition.LowerStageWait      => LowerStageWait,
    RobotPosition.LowerStageContact   => LowerStageContact,
    RobotPosition.UpperChamberWait    => UpperChamberWait,
    RobotPosition.UpperChamberContact => UpperChamberContact,
    RobotPosition.LowerChamberWait    => LowerChamberWait,
    RobotPosition.LowerChamberContact => LowerChamberContact,
    RobotPosition.Peel                => Peel,
    _                                 => pos.ToString(),
  };
}

/// <summary>
/// 포지션별 기본 이동 속도 (0-100 %).
/// 레시피에 값이 없을 경우 폴백으로만 사용.
/// </summary>
public static class RobotVelocityDefault
{
  public const int Home                = 30;
  public const int Ready               = 30;
  public const int UpperStageWait      = 60;
  public const int UpperStageContact   = 30;
  public const int LowerStageWait      = 60;
  public const int LowerStageContact   = 30;
  public const int UpperChamberWait    = 40;
  public const int UpperChamberContact = 20;
  public const int LowerChamberWait    = 40;
  public const int LowerChamberContact = 20;
  public const int Peel                = 20;

  public static int ForPosition(RobotPosition pos) => pos switch
  {
    RobotPosition.Home                => Home,
    RobotPosition.Ready               => Ready,
    RobotPosition.UpperStageWait      => UpperStageWait,
    RobotPosition.UpperStageContact   => UpperStageContact,
    RobotPosition.LowerStageWait      => LowerStageWait,
    RobotPosition.LowerStageContact   => LowerStageContact,
    RobotPosition.UpperChamberWait    => UpperChamberWait,
    RobotPosition.UpperChamberContact => UpperChamberContact,
    RobotPosition.LowerChamberWait    => LowerChamberWait,
    RobotPosition.LowerChamberContact => LowerChamberContact,
    RobotPosition.Peel                => Peel,
    _                                 => 50,
  };
}
