namespace Tcd.Simulator
{
    /// <summary>
    /// 시퀀스 키 상수. 값은 ID와 동일하게 정의.
    /// </summary>
    public static class TcdSequenceKeys
    {
        public const string Robot_Move_Stage = "Robot_Move_Stage";
        public const string Robot_Move_Home = "Robot_Move_Home";
        public const string Robot_Wait_Home = "Robot_Wait_Home";
        public const string Robot_Move_UpperLoad = "Robot_Move_UpperLoad";
        public const string Robot_Move_LowerLoad = "Robot_Move_LowerLoad";

        public const string Robot_Wait_Stage = "Robot_Wait_Stage";
        public const string Robot_Wait_UpperLoad = "Robot_Wait_UpperLoad";
        public const string Robot_Wait_LowerLoad = "Robot_Wait_LowerLoad";

        public const string Robot_Pick_Stage1 = "Robot_Pick_Stage1";
        public const string Robot_Pick_Stage2 = "Robot_Pick_Stage2";
        public const string Robot_Place_UpperChamber = "Robot_Place_UpperChamber";
        public const string Robot_Place_LowerChamber = "Robot_Place_LowerChamber";
        public const string Robot_Pick_LowerChamber = "Robot_Pick_LowerChamber";
        public const string Robot_Place_Stage2 = "Robot_Place_Stage2";

        public const string AxisU_Command_Zero = "AxisU_Command_Zero";
        public const string AxisV_Command_Zero = "AxisV_Command_Zero";
        public const string AxisW_Command_Zero = "AxisW_Command_Zero";
        public const string AxisU_Wait_Zero = "AxisU_Wait_Zero";
        public const string AxisV_Wait_Zero = "AxisV_Wait_Zero";
        public const string AxisW_Wait_Zero = "AxisW_Wait_Zero";

        public const string AxisZ_Command_Bond = "AxisZ_Command_Bond";
        public const string AxisZ_Wait_Bond = "AxisZ_Wait_Bond";
        public const string AxisZ_Command_Load = "AxisZ_Command_Load";
        public const string AxisZ_Wait_Load = "AxisZ_Wait_Load";

        public const string Plc_Wait_StageLoaded = "Plc_Wait_StageLoaded";

        public const string Manual_Axis0_AbsMove = "Manual_Axis0_AbsMove";
        public const string Manual_Axis0_Stop = "Manual_Axis0_Stop";
        public const string Manual_Axis1_AbsMove = "Manual_Axis1_AbsMove";
        public const string Manual_Axis1_Stop = "Manual_Axis1_Stop";

        // Manual Motor sequences (axis folders: Motor_U, Motor_V, Motor_W, Motor_ZLower, Motor_ZUpper)
        public const string Manual_Motor_U_AbsMove = "Manual_Motor_U_AbsMove";
        public const string Manual_Motor_U_IncMove = "Manual_Motor_U_IncMove";
        public const string Manual_Motor_U_JogMove = "Manual_Motor_U_JogMove";
        public const string Manual_Motor_U_Stop = "Manual_Motor_U_Stop";
        public const string Manual_Motor_U_Home = "Manual_Motor_U_Home";
        public const string Manual_Motor_U_FaultReset = "Manual_Motor_U_FaultReset";
        public const string Manual_Motor_U_ServoOn = "Manual_Motor_U_ServoOn";
        public const string Manual_Motor_U_ServoOff = "Manual_Motor_U_ServoOff";

        public const string Manual_Motor_V_AbsMove = "Manual_Motor_V_AbsMove";
        public const string Manual_Motor_V_IncMove = "Manual_Motor_V_IncMove";
        public const string Manual_Motor_V_JogMove = "Manual_Motor_V_JogMove";
        public const string Manual_Motor_V_Stop = "Manual_Motor_V_Stop";
        public const string Manual_Motor_V_Home = "Manual_Motor_V_Home";
        public const string Manual_Motor_V_FaultReset = "Manual_Motor_V_FaultReset";
        public const string Manual_Motor_V_ServoOn = "Manual_Motor_V_ServoOn";
        public const string Manual_Motor_V_ServoOff = "Manual_Motor_V_ServoOff";

        public const string Manual_Motor_W_AbsMove = "Manual_Motor_W_AbsMove";
        public const string Manual_Motor_W_IncMove = "Manual_Motor_W_IncMove";
        public const string Manual_Motor_W_JogMove = "Manual_Motor_W_JogMove";
        public const string Manual_Motor_W_Stop = "Manual_Motor_W_Stop";
        public const string Manual_Motor_W_Home = "Manual_Motor_W_Home";
        public const string Manual_Motor_W_FaultReset = "Manual_Motor_W_FaultReset";
        public const string Manual_Motor_W_ServoOn = "Manual_Motor_W_ServoOn";
        public const string Manual_Motor_W_ServoOff = "Manual_Motor_W_ServoOff";

        public const string Manual_Motor_ZLower_AbsMove = "Manual_Motor_ZLower_AbsMove";
        public const string Manual_Motor_ZLower_IncMove = "Manual_Motor_ZLower_IncMove";
        public const string Manual_Motor_ZLower_JogMove = "Manual_Motor_ZLower_JogMove";
        public const string Manual_Motor_ZLower_Stop = "Manual_Motor_ZLower_Stop";
        public const string Manual_Motor_ZLower_Home = "Manual_Motor_ZLower_Home";
        public const string Manual_Motor_ZLower_FaultReset = "Manual_Motor_ZLower_FaultReset";
        public const string Manual_Motor_ZLower_ServoOn = "Manual_Motor_ZLower_ServoOn";
        public const string Manual_Motor_ZLower_ServoOff = "Manual_Motor_ZLower_ServoOff";

        public const string Manual_Motor_ZUpper_AbsMove = "Manual_Motor_ZUpper_AbsMove";
        public const string Manual_Motor_ZUpper_IncMove = "Manual_Motor_ZUpper_IncMove";
        public const string Manual_Motor_ZUpper_JogMove = "Manual_Motor_ZUpper_JogMove";
        public const string Manual_Motor_ZUpper_Stop = "Manual_Motor_ZUpper_Stop";
        public const string Manual_Motor_ZUpper_Home = "Manual_Motor_ZUpper_Home";
        public const string Manual_Motor_ZUpper_FaultReset = "Manual_Motor_ZUpper_FaultReset";
        public const string Manual_Motor_ZUpper_ServoOn = "Manual_Motor_ZUpper_ServoOn";
        public const string Manual_Motor_ZUpper_ServoOff = "Manual_Motor_ZUpper_ServoOff";

        public const string Material_Create_Bonded = "Material_Create_Bonded";
        public const string Delay_Bond_Dwell1s = "Delay_Bond_Dwell1s";

        public const string SEMI_LoadUpperFilm = "SEMI_LoadUpperFilm";
        public const string SEMI_LoadLowerFilm = "SEMI_LoadLowerFilm";
        public const string SEMI_AlignUVW = "SEMI_AlignUVW";
        public const string SEMI_Bond = "SEMI_Bond";
        public const string SEMI_UnloadProductToStage2 = "SEMI_UnloadProductToStage2";

        public const string AUTO_Run = "AUTO_Run";

        /// <summary>레시피 기반 초기화. AUTO 전에 호출 가능.</summary>
        public const string Sequence_Init = "Sequence_Init";
    }
}
