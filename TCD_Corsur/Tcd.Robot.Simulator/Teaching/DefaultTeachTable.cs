namespace Tcd.Robot.Simulator.Teaching;

/// <summary>
/// SPEC_Layout_Recipe.md §4.3 / §5 기반 초기 티칭 테이블.
/// 셀 크기 2000×2000 mm. 좌표는 필드 조정 후 SetTeach 명령으로 덮어쓸 수 있다.
/// </summary>
public static class DefaultTeachTable
{
    /// <summary>최대 이동 속도 (100% 기준, mm/s)</summary>
    public const double MaxSpeedMmPerSec = 800.0;

    /// <summary>최소 이동 시뮬 시간 (ms) — 거리 0 이동 포함</summary>
    public const int MinMoveMs = 80;

    public static Dictionary<int, TeachPoint> Build() => new()
    {
        // ── 기본 안전 위치 ────────────────────────────────────────────────
        [0]  = new TeachPoint { PositionId = 0,  Name = "Home",
                                X = 1200, Y = 1000, Theta =   0, VelocityPct = 30 },
        [10] = new TeachPoint { PositionId = 10, Name = "Ready",
                                X = 1200, Y = 1000, Theta =   0, VelocityPct = 50 },

        // ── S1 (CGO, 상부 필름 스테이지) ─────────────────────────────────
        [11] = new TeachPoint { PositionId = 11, Name = "S1_PickupWait",
                                X = 1600, Y = 1300, Theta = -90, VelocityPct = 60 },
        [12] = new TeachPoint { PositionId = 12, Name = "S1_Pick",
                                X = 1700, Y = 1300, Theta = -90, VelocityPct = 30 },

        // ── S2 (OCA / LowStage, 하부 필름 스테이지) ──────────────────────
        [13] = new TeachPoint { PositionId = 13, Name = "S2_PickupWait",
                                X = 1600, Y =  700, Theta =  90, VelocityPct = 60 },
        [14] = new TeachPoint { PositionId = 14, Name = "S2_Pick",
                                X = 1700, Y =  700, Theta =  90, VelocityPct = 30 },

        // ── Upper Chamber (CGO 안착) ──────────────────────────────────────
        [15] = new TeachPoint { PositionId = 15, Name = "UpperChamber_PickupWait",
                                X =  700, Y = 1000, Theta =   0, VelocityPct = 60 },
        [16] = new TeachPoint { PositionId = 16, Name = "UpperChamber_Pick",
                                X =  550, Y = 1000, Theta =   0, VelocityPct = 30 },

        // ── Lower Chamber (OCA 안착 / 합착체 픽업) ───────────────────────
        [17] = new TeachPoint { PositionId = 17, Name = "LowerChamber_PickupWait",
                                X =  600, Y = 1000, Theta =   0, VelocityPct = 60 },
        [18] = new TeachPoint { PositionId = 18, Name = "LowerChamber_Pick",
                                X =  550, Y = 1000, Theta =   0, VelocityPct = 30 },

        // ── 박리 (Peel) ───────────────────────────────────────────────────
        [19] = new TeachPoint { PositionId = 19, Name = "Peel",
                                X =  550, Y = 1000, Theta =  45, VelocityPct = 20 },
    };
}
