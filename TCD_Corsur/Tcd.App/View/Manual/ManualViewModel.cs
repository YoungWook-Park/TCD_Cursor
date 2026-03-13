using Tcd.App.Mvvm;

namespace Tcd.App;

// Manual 탭 루트 뷰모델: 하위 Motor / Robot 뷰모델을 보유
public sealed class ManualViewModel : NotifyPropertyChangedBase
{
    public Manual_MotorViewModel Motor { get; } = new();
    public Manual_RobotViewModel Robot { get; } = new();
}

