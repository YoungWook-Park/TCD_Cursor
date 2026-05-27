using Tcd.App.Core;
using Tcd.App.Mvvm;

namespace Tcd.App;

/// <summary>
/// Settings 네비게이션 페이지: 로그 경로, 타임아웃 등 앱 환경설정.
/// 하드웨어 연결 설정은 Device 페이지에서 관리.
/// </summary>
public sealed class SettingsViewModel : NotifyPropertyChangedBase
{
  private readonly AppSettings _settings = MainCore.Instance.Settings;

  public string LogDirectory
  {
    get => _settings.LogDirectory;
    set
    {
      _settings.LogDirectory = value;
      Raise(nameof(LogDirectory));
    }
  }

  public int StageLoadTimeoutSec
  {
    get => (int)_settings.StageLoadTimeout.TotalSeconds;
    set
    {
      _settings.StageLoadTimeout = System.TimeSpan.FromSeconds(value);
      Raise(nameof(StageLoadTimeoutSec));
    }
  }

  public int RobotMoveTimeoutSec
  {
    get => (int)_settings.RobotMoveTimeout.TotalSeconds;
    set
    {
      _settings.RobotMoveTimeout = System.TimeSpan.FromSeconds(value);
      Raise(nameof(RobotMoveTimeoutSec));
    }
  }

  public int AxisMoveTimeoutSec
  {
    get => (int)_settings.AxisMoveTimeout.TotalSeconds;
    set
    {
      _settings.AxisMoveTimeout = System.TimeSpan.FromSeconds(value);
      Raise(nameof(AxisMoveTimeoutSec));
    }
  }
}
