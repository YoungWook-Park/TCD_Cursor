using Tcd.App.Mvvm;

namespace Tcd.App;

/// <summary>
/// Manual 탭 루트 ViewModel.
/// Motor / Robot / PLC 서브 탭을 호스팅.
/// 각 디바이스 연결 상태는 각 서브 뷰 내부에서 개별 표시.
/// </summary>
public sealed class ManualViewModel : NotifyPropertyChangedBase
{
  public Manual_MotorViewModel Motor { get; } = new();
  public Manual_RobotViewModel Robot { get; } = new();
  public PlcViewModel          Plc   { get; } = new();
}
