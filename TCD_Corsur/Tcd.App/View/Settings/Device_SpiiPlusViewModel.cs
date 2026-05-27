using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.App.Spii;

namespace Tcd.App;

public sealed class Device_SpiiPlusViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore _core = MainCore.Instance;
  private bool _isConnecting;
  private string _statusMessage;
  private string _ipAddress;
  private bool _useSimulator;

  private RelayCommand? cmd_Connect;
  private RelayCommand? cmd_Disconnect;

  public Device_SpiiPlusViewModel()
  {
    _ipAddress     = _core.Devices.SpiiIpAddress;
    _useSimulator  = !_core.Devices.UseSpiiPlus;
    _statusMessage = IsConnected ? "Connected" : "Simulation mode";
  }

  public string IpAddress
  {
    get => _ipAddress;
    set => Set(ref _ipAddress, value);
  }

  /// <summary>true = 소프트웨어 시뮬레이터 사용 (SimMotionService)</summary>
  public bool UseSimulator
  {
    get => _useSimulator;
    set
    {
      if (!Set(ref _useSimulator, value)) return;
      Raise(nameof(IsIpEnabled));
      cmd_Connect?.RaiseCanExecuteChanged();
      cmd_Disconnect?.RaiseCanExecuteChanged();
      if (value)
        ApplySwitchToSim();
    }
  }

  public bool IsIpEnabled => !_useSimulator && !_isConnecting;

  public bool IsConnected => _core.Motion is SpiiPlusMotionService;

  public bool IsConnecting
  {
    get => _isConnecting;
    private set
    {
      if (!Set(ref _isConnecting, value)) return;
      Raise(nameof(IsIpEnabled));
      cmd_Connect?.RaiseCanExecuteChanged();
      cmd_Disconnect?.RaiseCanExecuteChanged();
    }
  }

  public string StatusMessage
  {
    get => _statusMessage;
    private set => Set(ref _statusMessage, value);
  }

  public ICommand Cmd_Connect => cmd_Connect ??= new RelayCommand(
    _ => ConnectAsync(),
    _ => !IsConnected && !IsConnecting && !UseSimulator);

  public ICommand Cmd_Disconnect => cmd_Disconnect ??= new RelayCommand(
    _ => ApplySwitchToSim(),
    _ => IsConnected && !IsConnecting);

  private void ConnectAsync()
  {
    IsConnecting = true;
    StatusMessage = $"Connecting to {IpAddress}...";
    _ = Task.Run(() =>
    {
      try
      {
        _core.SwitchToSpiiPlus(IpAddress);
        _core.Devices.Save();
        SetStatus("Connected");
      }
      catch (Exception ex)
      {
        _core.SwitchToSimulation();
        SetStatus($"Connect failed: {ex.Message}");
      }
      finally
      {
        App.Current.Dispatcher.Invoke(() =>
        {
          IsConnecting = false;
          Raise(nameof(IsConnected));
          cmd_Connect?.RaiseCanExecuteChanged();
          cmd_Disconnect?.RaiseCanExecuteChanged();
        });
      }
    });
  }

  private void ApplySwitchToSim()
  {
    _core.SwitchToSimulation();
    _core.Devices.Save();
    Raise(nameof(IsConnected));
    StatusMessage = "Simulation mode";
    cmd_Connect?.RaiseCanExecuteChanged();
    cmd_Disconnect?.RaiseCanExecuteChanged();
  }

  private void SetStatus(string msg) =>
    App.Current.Dispatcher.Invoke(() => StatusMessage = msg);
}
