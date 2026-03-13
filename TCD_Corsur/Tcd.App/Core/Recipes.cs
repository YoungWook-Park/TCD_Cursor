using System.Text.Json.Serialization;
using Tcd.Devices;

namespace Tcd.App.Core;

public sealed class TcdRecipe
{
    public int Version { get; set; } = 1;

    public string Name { get; set; } = "Default";

    // Teaching positions (simple version)
    // Axis name -> target position
    public Dictionary<string, double> AxisTeach { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["U"] = 0,
        ["V"] = 0,
        ["W"] = 0,
        ["Z_Load"] = 0,
        ["Z_Bond"] = 100,
    };

    // Robot teach points are discrete positions for now
    public Dictionary<string, RobotPosition> RobotTeach { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Stage"] = RobotPosition.Stage,
        ["UpperLoad"] = RobotPosition.UpperChamberLoad,
        ["LowerLoad"] = RobotPosition.LowerChamberLoad,
    };

    public double GetAxis(string key, double fallback = 0)
        => AxisTeach.TryGetValue(key, out var v) ? v : fallback;

    public void SetAxis(string key, double value) => AxisTeach[key] = value;
}

