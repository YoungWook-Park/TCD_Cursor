namespace Tcd.App.Define;

/// <summary>
/// 모터 축별 고정 티칭 위치 이름 상수.
/// 시퀀스 내부에서 레시피 조회 시 키로 사용. 동적 추가/제거 불가.
/// </summary>
public static class MotorTeachKeys
{
  // 전 축 공통
  public const string Ready = "Ready";

  // ZLower 전용 (스테이지Z 상승 경로)
  public const string Pre_Lower_Contact = "Pre_Lower_Contact";
  public const string Lower_Contact     = "Lower_Contact";
  public const string Pre_Upper_Contact = "Pre_Upper_Contact";
  public const string Upper_Contact     = "Upper_Contact";
  public const string PressPos          = "PressPos";

  // UVW 공통
  public const string Vision_Align = "Vision_Align";

  // ZLower 정의 순서 (UI 표시 순서)
  public static readonly IReadOnlyList<string> ZLowerKeys = new[]
  {
    Ready,
    Pre_Lower_Contact,
    Lower_Contact,
    Pre_Upper_Contact,
    Upper_Contact,
    PressPos,
  };

  // UVW 공통 키 목록
  public static readonly IReadOnlyList<string> UvwKeys = new[]
  {
    Ready,
    Vision_Align,
  };

  // ZUpper 키 목록 (PressPos 는 ZUpper 에서도 독립 저장)
  public static readonly IReadOnlyList<string> ZUpperKeys = new[]
  {
    Ready,
    PressPos,
  };
}
