using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Mvvm;

namespace Tcd.App;

public sealed class TeachViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore _core = MainCore.Instance;
  private readonly DispatcherTimer _statusTimer;

  public ObservableCollection<Teach_AxisViewModel> AxisTabs { get; } = new();

  public ObservableCollection<Teach_AxisViewModel.AxisStatusItem>
    AxisStatuses { get; } = new();

  public TeachViewModel()
  {
    foreach (var axis in AxisDefine.InOrder)
      AxisStatuses.Add(
        new Teach_AxisViewModel.AxisStatusItem { AxisName = axis });

    AxisTabs.Add(new Teach_AxisViewModel(
      AxisDefine.ZLower, "ZLower", MotorTeachKeys.ZLowerKeys, AxisStatuses));

    foreach (var axis in new[] { AxisDefine.U, AxisDefine.V, AxisDefine.W })
      AxisTabs.Add(new Teach_AxisViewModel(
        axis, axis, MotorTeachKeys.UvwKeys, AxisStatuses));

    AxisTabs.Add(new Teach_AxisViewModel(
      AxisDefine.ZUpper, "ZUpper", MotorTeachKeys.ZUpperKeys, AxisStatuses));

    _statusTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(200)
    };
    _statusTimer.Tick += (_, _) => RefreshStatus();
    _statusTimer.Start();
  }

  private void RefreshStatus()
  {
    foreach (var item in AxisStatuses)
    {
      try
      {
        var s = _core.AxisStateProvider.GetAxisState(item.AxisName);
        item.Position   = s.Position;
        item.IsMoving   = s.IsMoving;
        item.IsFault    = s.IsFault;
        item.IsHome     = s.IsHome;
        item.IsServoOn  = s.IsServoOn;
        item.IsLimitPos = s.IsLimitPos;
        item.IsLimitNeg = s.IsLimitNeg;
      }
      catch { }
    }
  }
}
