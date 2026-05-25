using Tcd.App.Mvvm;

namespace Tcd.App;

public sealed class SettingsViewModel : NotifyPropertyChangedBase
{
  public Device_SpiiPlusViewModel SpiiPlus { get; } = new();
  public Device_RobotViewModel    Robot    { get; } = new();
  public Device_PlcViewModel      Plc      { get; } = new();
}
