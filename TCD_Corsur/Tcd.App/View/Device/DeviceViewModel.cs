using Tcd.App.Mvvm;

namespace Tcd.App;

/// <summary>
/// Device 네비게이션 페이지 루트 ViewModel.
/// 각 하드웨어 디바이스의 연결/설정을 탭별로 관리.
/// </summary>
public sealed class DeviceViewModel : NotifyPropertyChangedBase
{
  public Device_SpiiPlusViewModel SpiiPlus { get; } = new();
  public Device_RobotViewModel    Robot    { get; } = new();
  public Device_PlcViewModel      Plc      { get; } = new();
}
