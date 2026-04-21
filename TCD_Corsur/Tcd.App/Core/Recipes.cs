using System.Text.Json.Serialization;
using Tcd.App.Define;
using Tcd.Devices;

namespace Tcd.App.Core;

/// <summary>모델 - 레시피 계층: 하나의 모델이 여러 레시피를 가짐. 레시피에는 축별 티칭 정보가 들어감.</summary>
public sealed class TcdModel
{
    public string Name { get; set; } = "Default";

    /// <summary>이 모델에 속한 레시피 목록 (메모리상 구성, 저장소와 동기화)</summary>
    public List<TcdRecipe> Recipes { get; set; } = new();
}

public sealed class TcdRecipe
{
    public int Version { get; set; } = 1;

    /// <summary>소속 모델 이름. 계층: 모델 → 레시피</summary>
    public string ModelName { get; set; } = "Default";

    public string Name { get; set; } = "Default";

    /// <summary>축별 티칭 포지션. 키는 축 이름(U, V, W, ZLower, ZUpper)</summary>
    public Dictionary<string, double> AxisTeach { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [AxisDefine.U] = 0,
        [AxisDefine.V] = 0,
        [AxisDefine.W] = 0,
        [AxisDefine.ZLower] = 0,
        [AxisDefine.ZUpper] = 100,
    };

    // Robot teach points are discrete positions for now
    public Dictionary<string, RobotPosition> RobotTeach { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Stage"] = RobotPosition.Stage,
        ["UpperLoad"] = RobotPosition.UpperChamberLoad,
        ["LowerLoad"] = RobotPosition.LowerChamberLoad,
    };

    /// <summary>
    /// 포지션별 로봇 이동 속도 (1-100 %).
    /// 키는 RobotPositionName 상수. 사용자가 레시피에서 자유롭게 변경 가능.
    /// </summary>
    public Dictionary<string, int> RobotVelocity { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
    {
        [RobotPositionName.Home]                    = RobotVelocityDefault.Home,
        [RobotPositionName.Ready]                   = RobotVelocityDefault.Ready,
        [RobotPositionName.S1_PickupWait]           = RobotVelocityDefault.S1_PickupWait,
        [RobotPositionName.S1_Pick]                 = RobotVelocityDefault.S1_Pick,
        [RobotPositionName.S2_PickupWait]           = RobotVelocityDefault.S2_PickupWait,
        [RobotPositionName.S2_Pick]                 = RobotVelocityDefault.S2_Pick,
        [RobotPositionName.UpperChamber_PickupWait] = RobotVelocityDefault.UpperChamber_PickupWait,
        [RobotPositionName.UpperChamber_Pick]       = RobotVelocityDefault.UpperChamber_Pick,
        [RobotPositionName.LowerChamber_PickupWait] = RobotVelocityDefault.LowerChamber_PickupWait,
        [RobotPositionName.LowerChamber_Pick]       = RobotVelocityDefault.LowerChamber_Pick,
        [RobotPositionName.Peel]                    = RobotVelocityDefault.Peel,
    };

    public int GetRobotVelocity(string positionName) =>
        RobotVelocity.TryGetValue(positionName, out var v) ? v : 50;

    /// <summary>모터 기본 속도. 레시피에서 설정, 시퀀스 Init 또는 모션 서비스에서 참조.</summary>
    public double MotionVelocity { get; set; } = 100;
    public double MotionAcc { get; set; } = 1000;
    public double MotionDec { get; set; } = 1000;
    public double MotionJerk { get; set; } = 1000;

    /// <summary>축별 티칭 값. 레거시 키(Z_Load, Z_Bond) 자동 매핑.</summary>
    public double GetAxis(string key, double fallback = 0)
    {
        if (AxisTeach.TryGetValue(key, out var v)) return v;
        if (string.Equals(key, AxisDefine.ZLower, StringComparison.OrdinalIgnoreCase) && AxisTeach.TryGetValue("Z_Load", out v)) return v;
        if (string.Equals(key, AxisDefine.ZUpper, StringComparison.OrdinalIgnoreCase) && AxisTeach.TryGetValue("Z_Bond", out v)) return v;
        return fallback;
    }

    public void SetAxis(string key, double value) => AxisTeach[key] = value;
}

