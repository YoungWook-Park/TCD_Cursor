using System;
using System.IO;
using System.Text.Json;

namespace Tcd.App.Core;

public sealed class DeviceSettings
{
  // ── SPiiPlus 모션 컨트롤러 ─────────────────────────────────────────
  public bool   UseSpiiPlus   { get; set; } = false;
  public string SpiiIpAddress { get; set; } = "10.0.0.100";

  // ── TCP 로봇 시뮬레이터 ────────────────────────────────────────────
  public bool   UseRobot  { get; set; } = true;
  public string RobotHost { get; set; } = "127.0.0.1";
  public int    RobotPort { get; set; } = 7001;

  // ── TCP PLC 시뮬레이터 ─────────────────────────────────────────────
  public bool   UsePlc  { get; set; } = true;
  public string PlcHost { get; set; } = "127.0.0.1";
  public int    PlcPort { get; set; } = 7002;

  // ──────────────────────────────────────────────────────────────────

  private static string FilePath =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "TCD", "device.json");

  public static DeviceSettings Load()
  {
    try
    {
      var path = FilePath;
      if (File.Exists(path))
      {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DeviceSettings>(json) ?? new();
      }
    }
    catch { }
    return new();
  }

  public void Save()
  {
    try
    {
      var path = FilePath;
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      var opts = new JsonSerializerOptions { WriteIndented = true };
      File.WriteAllText(path, JsonSerializer.Serialize(this, opts));
    }
    catch { }
  }
}
