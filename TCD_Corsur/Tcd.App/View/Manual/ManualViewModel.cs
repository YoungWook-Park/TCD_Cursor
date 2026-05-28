using System;
using System.Windows.Threading;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.App.Spii;
using Tcd.Devices;

namespace Tcd.App;

/// <summary>
/// Manual 탭 루트 ViewModel.
/// 설비 제어(Motor)만 포함. 디바이스 연결 관리는 Device 페이지에서 수행.
/// </summary>
public sealed class ManualViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore _core = MainCore.Instance;
  private readonly DispatcherTimer _statusTimer;

  public Manual_MotorViewModel Motor { get; } = new();

  // ── 통신 상태 (읽기 전용) ──────────────────────────────────────────

  public bool   SpiiConnected  { get; private set; }
  public string SpiiStatus     { get; private set; } = "Simulation";

  public bool   RobotConnected { get; private set; }
  public string RobotStatus    { get; private set; } = "Disconnected";

  public bool   PlcConnected   { get; private set; }
  public string PlcStatus      { get; private set; } = "Disconnected";

  public ManualViewModel()
  {
    _statusTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(500)
    };
    _statusTimer.Tick += (_, _) => RefreshStatus();
    _statusTimer.Start();
  }

  private void RefreshStatus()
  {
    var spiiConn = _core.Motion is SpiiPlusMotionService;
    if (spiiConn != SpiiConnected)
    {
      SpiiConnected = spiiConn;
      SpiiStatus    = spiiConn ? "Connected" : "Simulation";
      Raise(nameof(SpiiConnected));
      Raise(nameof(SpiiStatus));
    }

    var robotConn = _core.RobotDevice.IsConnected;
    if (robotConn != RobotConnected)
    {
      RobotConnected = robotConn;
      RobotStatus    = robotConn ? "Connected" : "Disconnected";
      Raise(nameof(RobotConnected));
      Raise(nameof(RobotStatus));
    }

    var plcConn = _core.PlcDevice.IsConnected;
    if (plcConn != PlcConnected)
    {
      PlcConnected = plcConn;
      PlcStatus    = plcConn ? "Connected" : "Disconnected";
      Raise(nameof(PlcConnected));
      Raise(nameof(PlcStatus));
    }
  }
}
