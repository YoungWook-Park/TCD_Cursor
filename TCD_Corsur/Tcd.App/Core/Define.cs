namespace Tcd.App.Core;

/// <summary>
/// 모터 축 이름 및 순서. 인덱스: U=0, V=1, W=2, ZLower=3, ZUpper=4.
/// </summary>
public static class AxisDefine
{
    public const string U = "U";
    public const string V = "V";
    public const string W = "W";
    public const string ZLower = "ZLower";
    public const string ZUpper = "ZUpper";

    /// <summary>축 순서: U(0), V(1), W(2), ZLower(3), ZUpper(4)</summary>
    public static readonly string[] InOrder = { U, V, W, ZLower, ZUpper };
}

/// <summary>
/// SpiiPlus ascpl 변수명 매핑. 값은 ID와 동일하게 정의.
/// </summary>
public static class SpiiDefine
{
    // 모니터링/공통
    public const string ON_MONITORING_FLAG = "ON_MONITORING_FLAG";

    // 정수 명령 (배열 인덱스별)
    public const string RD_Ena_CMD = "RD_Ena_CMD";
    public const string RD_Disable_CMD = "RD_Disable_CMD";
    public const string RD_Halt_CMD = "RD_Halt_CMD";
    public const string RD_Fcle_CMD = "RD_Fcle_CMD";
    public const string RD_Abs_CMD = "RD_Abs_CMD";
    public const string RD_pJog_CMD = "RD_pJog_CMD";
    public const string RD_nJog_CMD = "RD_nJog_CMD";
    public const string RD_Home_CMD = "RD_Home_CMD";

    // 실수 (배열 인덱스별) - 목표/속도/가감속/저크
    public const string PC_ACS_DISTANCE = "PC_ACS_DISTANCE";
    public const string PC_ACS_VELOCITY = "PC_ACS_VELOCITY";
    public const string PC_ACS_ACC = "PC_ACS_ACC";
    public const string PC_ACS_DEC = "PC_ACS_DEC";
    public const string PC_ACS_JERK = "PC_ACS_JERK";

    // 상태 변수 접두사 (접미사에 축 인덱스 0~4)
    public const string ACS_PC_IS_MOVE_AXIS = "ACS_PC_IS_MOVE_AXIS";
    public const string ACS_PC_IS_FAULT_AXIS = "ACS_PC_IS_FAULT_AXIS";
    public const string ACS_PC_IS_HOME_AXIS = "ACS_PC_IS_HOME_AXIS";
    public const string ACS_PC_CURRENT_POS_AXIS = "ACS_PC_CURRENT_POS_AXIS";

    // 기본 모션 값 (레시피 연동 전 임시)
    public const double DefaultVelocity = 100;
    public const double DefaultAcc = 1000;
    public const double DefaultDec = 1000;
    public const double DefaultJerk = 1000;
}
