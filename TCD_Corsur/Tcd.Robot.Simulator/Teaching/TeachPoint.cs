namespace Tcd.Robot.Simulator.Teaching;

/// <summary>하나의 티칭 포지션 데이터. ID·이름·좌표·개별 속도를 보관한다.</summary>
public sealed class TeachPoint
{
    public int    PositionId  { get; set; }
    public string Name        { get; set; } = "";
    /// <summary>X 좌표 (mm)</summary>
    public double X           { get; set; }
    /// <summary>Y 좌표 (mm)</summary>
    public double Y           { get; set; }
    /// <summary>회전각 (deg)</summary>
    public double Theta       { get; set; }
    /// <summary>이동 속도 퍼센트 0~100</summary>
    public int    VelocityPct { get; set; } = 50;
}
