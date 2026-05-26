using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;

namespace Tcd.App;

public sealed class RobotViewModel : NotifyPropertyChangedBase
{
  #region Variable

  private readonly MainCore   _core = MainCore.Instance;
  private readonly IRobotDevice _robot;

  private string _host;
  private int    _port;
  private bool   _isConnecting;
  private int    _velocity = 50;
  private string _logStatus = "";

  private bool          _isConnected;
  private bool          _isRunning;
  private bool          _isHome;
  private bool          _isError;
  private RobotPosition _currentPosition;
  private string        _errorMessage = "";

  #endregion

  #region Constructor

  public RobotViewModel()
  {
    _robot = _core.RobotDevice;
    _host  = _core.Settings.RobotSimHost;
    _port  = _core.Settings.RobotSimPort;

    _robot.StateChanged += OnStateChanged;
  }

  #endregion

  #region Properties

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

  public int Velocity
  {
    get => _velocity;
    set => Set(ref _velocity, Math.Clamp(value, 1, 100));
  }

  public bool IsConnecting
  {
    get => _isConnecting;
    private set
    {
      if (!Set(ref _isConnecting, value)) return;
      RaiseAllCanExecute();
    }
  }

  public bool IsConnected
  {
    get => _isConnected;
    private set
    {
      if (!Set(ref _isConnected, value)) return;
      RaiseAllCanExecute();
    }
  }

  public bool IsRunning
  {
    get => _isRunning;
    private set
    {
      if (!Set(ref _isRunning, value)) return;
      RaiseAllCanExecute();
    }
  }

  public bool IsHome
  {
    get => _isHome;
    private set => Set(ref _isHome, value);
  }

  public bool IsError
  {
    get => _isError;
    private set => Set(ref _isError, value);
  }

  public RobotPosition CurrentPosition
  {
    get => _currentPosition;
    private set => Set(ref _currentPosition, value);
  }

  public string ErrorMessage
  {
    get => _errorMessage;
    private set => Set(ref _errorMessage, value);
  }

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  #endregion

  #region State Handler

  private void OnStateChanged(object? sender, RobotDeviceStateArgs e)
  {
    Application.Current?.Dispatcher.Invoke(() =>
    {
      IsConnected     = e.IsConnected;
      IsRunning       = e.IsRunning;
      IsHome          = e.IsHome;
      IsError         = e.IsError;
      CurrentPosition = e.CurrentPosition;
      ErrorMessage    = e.ErrorMessage ?? "";

      if (!string.IsNullOrEmpty(e.ErrorMessage))
        LogStatus = $"[Error] {e.ErrorMessage}";
    });
  }

  #endregion

  #region Commands — Connection

  private RelayCommand? cmd_Connect;
  public ICommand Cmd_Connect => cmd_Connect ??=
    new RelayCommand(_ => ConnectAsync(), _ => !IsConnected && !IsConnecting);

  private RelayCommand? cmd_Disconnect;
  public ICommand Cmd_Disconnect => cmd_Disconnect ??=
    new RelayCommand(_ => Disconnect(), _ => IsConnected);

  private void ConnectAsync()
  {
    IsConnecting = true;
    LogStatus = $"Connecting to {Host}:{Port}...";
    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.ConnectAsync(Host, Port).ConfigureAwait(false);
        SetLog($"Connected to {Host}:{Port}");
      }
      catch (Exception ex)
      {
        SetLog($"Connect failed: {ex.Message}");
      }
      finally
      {
        Application.Current?.Dispatcher.Invoke(() => IsConnecting = false);
      }
    });
  }

  private void Disconnect()
  {
    _robot.Disconnect();
    SetLog("Disconnected");
  }

  #endregion

  #region Commands — Stop

  private RelayCommand? cmd_Stop;
  public ICommand Cmd_Stop => cmd_Stop ??=
    new RelayCommand(_ => FireStop(), _ => IsConnected && IsRunning);

  private void FireStop()
  {
    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.StopAsync().ConfigureAwait(false);
        SetLog("Stop sent");
      }
      catch (Exception ex) { SetLog($"Stop error: {ex.Message}"); }
    });
  }

  #endregion

  #region Commands — Move

  private bool CanMove => IsConnected && !IsRunning;

  private RelayCommand? cmd_MoveHome;
  public ICommand Cmd_MoveHome => cmd_MoveHome ??=
    new RelayCommand(_ => FireMove(RobotPosition.Home), _ => CanMove);

  private RelayCommand? cmd_MoveReady;
  public ICommand Cmd_MoveReady => cmd_MoveReady ??=
    new RelayCommand(_ => FireMove(RobotPosition.Ready), _ => CanMove);

  private RelayCommand? cmd_MoveS1PickupWait;
  public ICommand Cmd_MoveS1PickupWait => cmd_MoveS1PickupWait ??=
    new RelayCommand(_ => FireMove(RobotPosition.S1_PickupWait), _ => CanMove);

  private RelayCommand? cmd_MoveS1Pick;
  public ICommand Cmd_MoveS1Pick => cmd_MoveS1Pick ??=
    new RelayCommand(_ => FireMove(RobotPosition.S1_Pick), _ => CanMove);

  private RelayCommand? cmd_MoveS2PickupWait;
  public ICommand Cmd_MoveS2PickupWait => cmd_MoveS2PickupWait ??=
    new RelayCommand(_ => FireMove(RobotPosition.S2_PickupWait), _ => CanMove);

  private RelayCommand? cmd_MoveS2Pick;
  public ICommand Cmd_MoveS2Pick => cmd_MoveS2Pick ??=
    new RelayCommand(_ => FireMove(RobotPosition.S2_Pick), _ => CanMove);

  private RelayCommand? cmd_MoveUCPickupWait;
  public ICommand Cmd_MoveUCPickupWait => cmd_MoveUCPickupWait ??=
    new RelayCommand(
      _ => FireMove(RobotPosition.UpperChamber_PickupWait), _ => CanMove);

  private RelayCommand? cmd_MoveUCPick;
  public ICommand Cmd_MoveUCPick => cmd_MoveUCPick ??=
    new RelayCommand(
      _ => FireMove(RobotPosition.UpperChamber_Pick), _ => CanMove);

  private RelayCommand? cmd_MoveLCPickupWait;
  public ICommand Cmd_MoveLCPickupWait => cmd_MoveLCPickupWait ??=
    new RelayCommand(
      _ => FireMove(RobotPosition.LowerChamber_PickupWait), _ => CanMove);

  private RelayCommand? cmd_MoveLCPick;
  public ICommand Cmd_MoveLCPick => cmd_MoveLCPick ??=
    new RelayCommand(
      _ => FireMove(RobotPosition.LowerChamber_Pick), _ => CanMove);

  private RelayCommand? cmd_MovePeel;
  public ICommand Cmd_MovePeel => cmd_MovePeel ??=
    new RelayCommand(_ => FireMove(RobotPosition.Peel), _ => CanMove);

  private void FireMove(RobotPosition pos)
  {
    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.SetVelocityAsync(pos, _velocity).ConfigureAwait(false);
        var ok = await _robot.MoveAsync(pos).ConfigureAwait(false);
        SetLog(ok
          ? $"Move → {pos}"
          : $"Move rejected (running or error) → {pos}");
      }
      catch (Exception ex) { SetLog($"Move error: {ex.Message}"); }
    });
  }

  #endregion

  #region Helpers

  private void SetLog(string msg) =>
    Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);

  private void RaiseAllCanExecute()
  {
    cmd_Connect?.RaiseCanExecuteChanged();
    cmd_Disconnect?.RaiseCanExecuteChanged();
    cmd_Stop?.RaiseCanExecuteChanged();
    cmd_MoveHome?.RaiseCanExecuteChanged();
    cmd_MoveReady?.RaiseCanExecuteChanged();
    cmd_MoveS1PickupWait?.RaiseCanExecuteChanged();
    cmd_MoveS1Pick?.RaiseCanExecuteChanged();
    cmd_MoveS2PickupWait?.RaiseCanExecuteChanged();
    cmd_MoveS2Pick?.RaiseCanExecuteChanged();
    cmd_MoveUCPickupWait?.RaiseCanExecuteChanged();
    cmd_MoveUCPick?.RaiseCanExecuteChanged();
    cmd_MoveLCPickupWait?.RaiseCanExecuteChanged();
    cmd_MoveLCPick?.RaiseCanExecuteChanged();
    cmd_MovePeel?.RaiseCanExecuteChanged();
  }

  #endregion
}
