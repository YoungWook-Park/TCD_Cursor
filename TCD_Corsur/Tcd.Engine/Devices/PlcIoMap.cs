namespace Tcd.Devices
{
  /// <summary>
  /// SPEC_Control_IO.md 기준 DI 비트 주소 (PLC → WPF).
  /// addr = byteNum * 8 + bitNum
  /// </summary>
  public enum DiBit
  {
    EStop_OK            = 0,   // B0.0
    DoorClosed          = 1,   // B0.1
    LowStageVac         = 4,   // B0.4
    HighStageVac        = 5,   // B0.5
    RobotGripVac        = 6,   // B0.6
    UpperChamberVac     = 7,   // B0.7
    LowerChamberAtReady = 10,  // B1.2
    UpperChamberAtReady = 11,  // B1.3
    LowerChamberAtBond  = 12,  // B1.4
    UpperChamberAtBond  = 13,  // B1.5
    MaterialLowStage    = 14,  // B1.6
    MaterialHighStage   = 15,  // B1.7
    AtAtmospheric       = 16,  // B2.0
  }

  /// <summary>
  /// DO 비트 주소 (WPF → PLC).
  /// </summary>
  public enum DoBit
  {
    VacPumpRequest     = 24,  // B3.0
    LowStageVacOn      = 25,  // B3.1
    HighStageVacOn     = 26,  // B3.2
    RobotGripVacOn     = 27,  // B3.3
    UpperChamberVacOn  = 28,  // B3.4
    EscEnable          = 29,  // B3.5
    ChamberMoveToBond  = 34,  // B4.2
    ChamberMoveToReady = 35,  // B4.3
    LaminationActive   = 36,  // B4.4
    VentValveOpen      = 42,  // B5.2
  }

  /// <summary>
  /// 핸드셰이크 비트 주소 (B6.*). 운전원/WPF/PLC 혼합 소유.
  /// </summary>
  public enum HsBit
  {
    Cmd_Start       = 48,  // B6.0 — 운전원
    Cmd_Stop        = 49,  // B6.1 — 운전원
    Sts_SeqRunning  = 50,  // B6.2 — WPF
    Sts_SeqComplete = 51,  // B6.3 — WPF
    Sts_SeqFault    = 52,  // B6.4 — WPF
    Alm_Any         = 54,  // B6.6 — PLC
  }

  /// <summary>
  /// AI 워드 주소 (PLC → WPF).
  /// </summary>
  public enum AiWord
  {
    ChamberPressure = 0,  // W0  kPa×100
    Loadcell        = 2,  // W2  N×10
    ChamberVacuum   = 4,  // W4  kPa×10  ← 진공도
    RobotX          = 6,  // W6  mm
    RobotY          = 7,  // W7  mm
    RobotTheta      = 8,  // W8  mdeg
  }

  /// <summary>
  /// AO 워드 주소 (WPF → PLC).
  /// </summary>
  public enum AoWord
  {
    EscVoltage = 20,  // W20 V×100
    SeqStepId  = 22,  // W22
    UvwCorrU   = 23,  // W23 μm
    UvwCorrV   = 24,  // W24 μm
    UvwCorrW   = 25,  // W25 mdeg
  }
}
