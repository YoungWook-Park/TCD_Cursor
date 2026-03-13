namespace Tcd.Simulator
{
    public static class TcdSequenceKeys
    {
        // Manual (single-axis / single-device)
        public const string Robot_Move_Stage = "MAN.Robot.Move.Stage";
        public const string Robot_Move_UpperLoad = "MAN.Robot.Move.UpperLoad";
        public const string Robot_Move_LowerLoad = "MAN.Robot.Move.LowerLoad";

        public const string Robot_Wait_Stage = "MAN.Robot.Wait.Stage";
        public const string Robot_Wait_UpperLoad = "MAN.Robot.Wait.UpperLoad";
        public const string Robot_Wait_LowerLoad = "MAN.Robot.Wait.LowerLoad";

        public const string Robot_Pick_Stage1 = "MAN.Robot.Pick.Stage1";
        public const string Robot_Pick_Stage2 = "MAN.Robot.Pick.Stage2";
        public const string Robot_Place_UpperChamber = "MAN.Robot.Place.UpperChamber";
        public const string Robot_Place_LowerChamber = "MAN.Robot.Place.LowerChamber";
        public const string Robot_Pick_LowerChamber = "MAN.Robot.Pick.LowerChamber";
        public const string Robot_Place_Stage2 = "MAN.Robot.Place.Stage2";

        public const string AxisU_Command_Zero = "MAN.Axis.U.Cmd.Zero";
        public const string AxisV_Command_Zero = "MAN.Axis.V.Cmd.Zero";
        public const string AxisW_Command_Zero = "MAN.Axis.W.Cmd.Zero";
        public const string AxisU_Wait_Zero = "MAN.Axis.U.Wait.Zero";
        public const string AxisV_Wait_Zero = "MAN.Axis.V.Wait.Zero";
        public const string AxisW_Wait_Zero = "MAN.Axis.W.Wait.Zero";

        public const string AxisZ_Command_Bond = "MAN.Axis.Z.Cmd.Bond";
        public const string AxisZ_Wait_Bond = "MAN.Axis.Z.Wait.Bond";
        public const string AxisZ_Command_Load = "MAN.Axis.Z.Cmd.Load";
        public const string AxisZ_Wait_Load = "MAN.Axis.Z.Wait.Load";

        public const string Plc_Wait_StageLoaded = "MAN.Plc.Wait.StageLoaded";

        /// <summary>0번 모터(U) 절대이동. parameter: double target position.</summary>
        public const string Manual_Axis0_AbsMove = "Manual.Axis0.AbsMove";
        /// <summary>0번 모터(U) 정지.</summary>
        public const string Manual_Axis0_Stop = "Manual.Axis0.Stop";

        public const string Material_Create_Bonded = "MAN.Material.Create.Bonded";
        public const string Delay_Bond_Dwell1s = "MAN.Delay.Bond.1s";

        // Semi-auto (feature blocks)
        public const string SEMI_LoadUpperFilm = "SEMI.LoadUpperFilm";
        public const string SEMI_LoadLowerFilm = "SEMI.LoadLowerFilm";
        public const string SEMI_AlignUVW = "SEMI.AlignUVW";
        public const string SEMI_Bond = "SEMI.Bond";
        public const string SEMI_UnloadProductToStage2 = "SEMI.UnloadProductToStage2";

        // Auto
        public const string AUTO_Run = "AUTO.Run";
    }
}

