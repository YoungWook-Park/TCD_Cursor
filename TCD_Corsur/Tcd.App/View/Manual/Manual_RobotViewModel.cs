using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Mvvm;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

/// <summary>
/// 로봇 수동 제어 ViewModel.
/// 연결 관리 및 IO 상태 표시를 담당.
/// 이동 명령은 SequenceManager 경유 (인터락은 Manual_Robot 시퀀스 내부에서 처리).
/// </summary>
public sealed class Manual_RobotViewModel : NotifyPropertyChangedBase
{
  #region Fields

  private readonly MainCore     _core  = MainCore.Instance;
  private readonly IRobotDevice _robot;
  private CancellationTokenSource? _activeCts;

  private string _host;
  private int    _port;
  private bool   _isConnecting;
  private int    _velocity = 50;
  private string _logStatus = "";

  // IO 신호 상태 (표시 전용 — StateChanged 이벤트로 갱신)
  private bool _ioStartOn;
  private bool _ioRunning;
  private bool _ioMoving;
  private bool _ioComplete;

  private bool          _isConnected;
  private bool          _isError;
  private RobotPosition _currentPosition;
  private string        _errorMessage = "";

  private RobotProgramItem? _selectedProgramItem;

  #endregion

  #region Constructor

  public Manual_RobotViewModel()
  {
    _robot = _core.RobotDevice;
    _host  = _core.Devices.RobotHost;
    _port  = _core.Devices.RobotPort;

    _robot.StateChanged += OnRobotStateChanged;

    Programs = new ObservableCollection<RobotProgramItem>(
      Enum.GetValues<RobotPosition>()
        .Where(p => SequenceKeyFor(p) != null)
        .Select(p => new RobotProgramItem(p)));

    _selectedProgramItem = Programs.FirstOrDefault(
      p => p.Position == RobotPosition.Home) ?? Programs.FirstOrDefault();
  }

  #endregion

  #region Properties — Connection

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

  public bool IsConnecting
  {
    get => _isConnecting;
    private set { if (Set(ref _isConnecting, value)) RaiseCanExecute(); }
  }

  public bool IsConnected
  {
    get => _isConnected;
    private set { if (Set(ref _isConnected, value)) RaiseCanExecute(); }
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

  #endregion

  #region Properties — IO Signals (표시 전용)

  public bool IoStartOn
  {
    get => _ioStartOn;
    private set => Set(ref _ioStartOn, value);
  }

  public bool IoRunning
  {
    get => _ioRunning;
    private set { if (Set(ref _ioRunning, value)) RaiseCanExecute(); }
  }

  public bool IoMoving
  {
    get => _ioMoving;
    private set { if (Set(ref _ioMoving, value)) RaiseCanExecute(); }
  }

  public bool IoComplete
  {
    get => _ioComplete;
    private set => Set(ref _ioComplete, value);
  }

  #endregion

  #region Properties — Motion

  public int Velocity
  {
    get => _velocity;
    set => Set(ref _velocity, Math.Clamp(value, 1, 100));
  }

  public ObservableCollection<RobotProgramItem> Programs { get; }

  public RobotProgramItem? SelectedProgramItem
  {
    get => _selectedProgramItem;
    set => Set(ref _selectedProgramItem, value);
  }

  private RobotPosition SelectedPosition =>
    _selectedProgramItem?.Position ?? RobotPosition.Home;

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  #endregion

  #region State Handler

  private void OnRobotStateChanged(object? sender, RobotDeviceStateArgs e)
  {
    Application.Current?.Dispatcher.Invoke(() =>
    {
      var wasConnected = IsConnected;

      IsConnected     = e.IsConnected;
      IsError         = e.IsError;
      CurrentPosition = e.CurrentPosition;
      ErrorMessage    = e.ErrorMessage ?? "";

      if (wasConnected && !e.IsConnected)
        ResetDisplaySignals();

      if (e.IsError)
        LogStatus = $"[Error] {e.ErrorMessage}";

      // 이동 완료 감지
      if (IoMoving && !e.IsRunning)
        OnMoveCompleted();
    });
  }

  private void OnMoveCompleted()
  {
    IoMoving   = false;
    IoComplete = true;
    LogStatus  = $"Complete: {CurrentPosition}";

    Task.Delay(300).ContinueWith(_ =>
      Application.Current?.Dispatcher.Invoke(() =>
      {
        IoComplete = false;
        LogStatus  = $"Ready — {CurrentPosition}";
      }));
  }

  private void ResetDisplaySignals()
  {
    IoStartOn  = false;
    IoRunning  = false;
    IoMoving   = false;
    IoComplete = false;
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
    LogStatus = $"Connecting {Host}:{Port}...";

    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.ConnectAsync(_host, _port).ConfigureAwait(false);
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
    ResetDisplaySignals();
    _robot.Disconnect();
    SetLog("Disconnected");
  }

  #endregion

  #region Commands — Init

  private RelayCommand? cmd_InitStartIo;
  public ICommand Cmd_InitStartIo => cmd_InitStartIo ??=
    new RelayCommand(_ => InitStartIo(), _ => IsConnected && !IoRunning);

  private void InitStartIo()
  {
    LogStatus = "Init: Start IO On...";
    IoStartOn = true;

    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.SetVelocityAsync(RobotPosition.Ready, 30)
          .ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoRunning = true;
          LogStatus = "Running IO On — Ready for operation";
        });
      }
      catch (Exception ex)
      {
        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoStartOn = false;
          LogStatus = $"Init failed: {ex.Message}";
        });
      }
    });
  }

  #endregion

  #region Commands — Move (시퀀스 경유)

  private RelayCommand? cmd_Move;
  public ICommand Cmd_Move => cmd_Move ??=
    new RelayCommand(
      _ => MoveToSelected(),
      _ => IsConnected && IoRunning && !IoMoving);

  private void MoveToSelected()
  {
    var seqKey = SequenceKeyFor(SelectedPosition);
    if (seqKey == null)
    {
      LogStatus = $"No sequence for {SelectedPosition}";
      return;
    }

    _activeCts?.Cancel();
    _activeCts?.Dispose();
    var cts = new CancellationTokenSource();
    _activeCts = cts;

    IoMoving  = true;
    LogStatus = $"Go → {RobotPositionName.FromPosition(SelectedPosition)} (vel={Velocity}%)";

    _ = Task.Run(async () =>
    {
      try
      {
        var result = await _core.Sequences
          .RunAsync(seqKey, _core.Simulation, Velocity, cts.Token)
          .ConfigureAwait(false);

        SetLog(result.Status == SequenceStatus.Succeeded
          ? $"OK: {RobotPositionName.FromPosition(SelectedPosition)}"
          : $"FAIL: {result.Error ?? seqKey}");
      }
      catch (OperationCanceledException)
      {
        SetLog("Move cancelled");
      }
      catch (Exception ex)
      {
        SetLog($"[Error] {ex.Message}");
      }
      finally
      {
        Application.Current?.Dispatcher.Invoke(() => IoMoving = false);
      }
    });
  }

  #endregion

  #region Commands — Stop (비상 정지 — 직접 호출)

  private RelayCommand? cmd_Stop;
  public ICommand Cmd_Stop => cmd_Stop ??=
    new RelayCommand(_ => Stop(), _ => IsConnected);

  private void Stop()
  {
    _activeCts?.Cancel();

    _ = Task.Run(async () =>
    {
      try
      {
        await _robot.StopAsync().ConfigureAwait(false);
        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoMoving  = false;
          LogStatus = "Stop sent";
        });
      }
      catch (Exception ex) { SetLog($"Stop error: {ex.Message}"); }
    });
  }

  #endregion

  #region Helpers

  private static string? SequenceKeyFor(RobotPosition pos) => pos switch
  {
    RobotPosition.Home                => TcdSequenceKeys.Manual_Robot_Home,
    RobotPosition.UpperStageWait      => TcdSequenceKeys.Manual_Robot_UpperStageWait,
    RobotPosition.UpperStageContact   => TcdSequenceKeys.Manual_Robot_UpperStageContact,
    RobotPosition.LowerStageWait      => TcdSequenceKeys.Manual_Robot_LowerStageWait,
    RobotPosition.LowerStageContact   => TcdSequenceKeys.Manual_Robot_LowerStageContact,
    RobotPosition.UpperChamberWait    => TcdSequenceKeys.Manual_Robot_UpperChamberWait,
    RobotPosition.UpperChamberContact => TcdSequenceKeys.Manual_Robot_UpperChamberContact,
    RobotPosition.LowerChamberWait    => TcdSequenceKeys.Manual_Robot_LowerChamberWait,
    RobotPosition.LowerChamberContact => TcdSequenceKeys.Manual_Robot_LowerChamberContact,
    _                                 => null,
  };

  private void SetLog(string msg) =>
    Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);

  private void RaiseCanExecute()
  {
    cmd_Connect?.RaiseCanExecuteChanged();
    cmd_Disconnect?.RaiseCanExecuteChanged();
    cmd_InitStartIo?.RaiseCanExecuteChanged();
    cmd_Move?.RaiseCanExecuteChanged();
    cmd_Stop?.RaiseCanExecuteChanged();
  }

  #endregion

  // ── Inner type ───────────────────────────────────────────────────────────

  public sealed class RobotProgramItem
  {
    public RobotPosition Position { get; }
    public string        Label    { get; }

    public RobotProgramItem(RobotPosition pos)
    {
      Position = pos;
      Label    = RobotPositionName.FromPosition(pos);
    }

    public override string ToString() => Label;
  }
}
