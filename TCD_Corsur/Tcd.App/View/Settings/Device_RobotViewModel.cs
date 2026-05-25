using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;

namespace Tcd.App;

public sealed class Device_RobotViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore _core = MainCore.Instance;
  private readonly IRobotDevice _robot;
  private bool _isConnecting;
  private string _host;
  private int _port;
  private string _statusMessage;

  private RelayCommand? cmd_Connect;
  private RelayCommand? cmd_Disconnect;

  public Device_RobotViewModel()
  {
    _robot = _core.RobotDevice;
    _host  = _core.Settings.RobotSimHost;
    _port  = _core.Settings.RobotSimPort;
    _statusMessage = _robot.IsConnected ? "Connected" : "Disconnected";
    _robot.StateChanged += OnRobotStateChanged;
  }

  public string Host
  {
    get => _host;
    set => Set(ref _host, value);
  }

  public int Port
  {
    get => _port;
    set => Set(ref _port, value);
  }

  public bool IsConnected => _robot.IsConnected;

  public bool IsConnecting
  {
    get => _isConnecting;
    private set
    {
      if (!Set(ref _isConnecting, value)) return;
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
    _ => !IsConnected && !IsConnecting);

  public ICommand Cmd_Disconnect => cmd_Disconnect ??= new RelayCommand(
    _ => _robot.Disconnect(),
    _ => IsConnected && !IsConnecting);

  private void ConnectAsync()
  {
    IsConnecting = true;
    StatusMessage = $"Connecting to {Host}:{Port}...";
    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.ConnectAsync(Host, Port).ConfigureAwait(false);
        SetStatus($"Connected to {Host}:{Port}");
      }
      catch (Exception ex)
      {
        SetStatus($"Connect failed: {ex.Message}");
      }
      finally
      {
        App.Current?.Dispatcher.Invoke(() =>
        {
          IsConnecting = false;
          Raise(nameof(IsConnected));
          cmd_Connect?.RaiseCanExecuteChanged();
          cmd_Disconnect?.RaiseCanExecuteChanged();
        });
      }
    });
  }

  private void OnRobotStateChanged(object? sender, RobotDeviceStateArgs e)
  {
    App.Current?.Dispatcher.Invoke(() =>
    {
      Raise(nameof(IsConnected));
      cmd_Connect?.RaiseCanExecuteChanged();
      cmd_Disconnect?.RaiseCanExecuteChanged();
      if (e.IsError)
        StatusMessage = $"[Error] {e.ErrorMessage}";
      else if (_robot.IsConnected)
        StatusMessage = "Connected";
      else
        StatusMessage = "Disconnected";
    });
  }

  private void SetStatus(string msg) =>
    App.Current?.Dispatcher.Invoke(() => StatusMessage = msg);
}
