namespace Tcd.App.Define;

/// <summary>
/// 알람 키 문자열 상수.
/// context.Alarms.Raise(new Alarm(...)) 첫 번째 인자에 사용.
/// </summary>
public static class AlarmKeys
{
  public const string ChamberNotEmpty = "CHAMBER_NOT_EMPTY";
  public const string RobotNotAtHome  = "ROBOT_NOT_AT_HOME";
}
