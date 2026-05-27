using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Devices;
using Tcd.App.Mvvm;

namespace Tcd.App;

public sealed class Device_PlcViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore    _core = MainCore.Instance;
  private readonly PlcTcpClient _plc;
  private bool   _isConnecting;
  private string _host;
  private int    _port;
  private string _statusMessage;

  private RelayCommand? cmd_Connect;
  private RelayCommand? cmd_Disconnect;

  public Device_PlcViewModel()
  {
    _plc           = _core.PlcDevice;
    _host          = _core.Devices.PlcHost;
    _port          = _core.Devices.PlcPort;
    _statusMessage = _plc.IsConnected ? "Connected" : "Disconnected";
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

  public bool IsConnected => _plc.IsConnected;

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
    _ => Disconnect(),
    _ => IsConnected && !IsConnecting);

  private void ConnectAsync()
  {
    IsConnecting = true;
    StatusMessage = $"Connecting to {Host}:{Port}...";
    _ = Task.Run(async () =>
    {
      try
      {
        await _plc.ConnectAsync(Host, Port).ConfigureAwait(false);
        _core.Devices.PlcHost = Host;
        _core.Devices.PlcPort = Port;
        _core.Devices.Save();
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

  private void Disconnect()
  {
    _plc.Disconnect();
    Raise(nameof(IsConnected));
    StatusMessage = "Disconnected";
    cmd_Connect?.RaiseCanExecuteChanged();
    cmd_Disconnect?.RaiseCanExecuteChanged();
  }

  private void SetStatus(string msg) =>
    App.Current?.Dispatcher.Invoke(() => StatusMessage = msg);
}
