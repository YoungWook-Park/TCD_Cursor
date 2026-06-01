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

namespace Tcd.App;

/// <summary>
/// 로봇 IO 핸드셰이크 기반 수동 제어 ViewModel.
///
/// 초기화 흐름:
///   Connect → [Init] → IoStartOn=true, IoRunning=true (동작 준비 완료)
///
/// 동작 흐름:
///   SetVelocity + WriteProgramNo → [Go] → IoGoAck=true, IoMoving=true
///   WaitForPosition → IoMoving=false, IoComplete=true
///   자동: IoGoAck=false (Go Off) → IoComplete=false (Complete Off)
///
/// 비정상 종료:
///   [Stop] → IoMoving=false, 모든 신호 Off
///   [Reset] → 모든 신호 Off + IoRunning=true (다음 동작 대기)
/// </summary>
public sealed class Manual_RobotViewModel : NotifyPropertyChangedBase
{
  #region Fields

  private readonly MainCore    _core  = MainCore.Instance;
  private readonly IRobotDevice _robot;
  private CancellationTokenSource? _activeCts;

  private string _host;
  private int    _port;
  private bool   _isConnecting;
  private int    _velocity = 50;
  private string _logStatus = "";

  // IO 신호 상태
  private bool _ioStartOn;
  private bool _ioRunning;
  private bool _ioMoving;
  private bool _ioGoAck;
  private bool _ioComplete;

  // 로봇 상태 미러
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
        .Select(p => new RobotProgramItem(p)));

    _selectedProgramItem = Programs.FirstOrDefault(
      p => p.Position == RobotPosition.Ready) ?? Programs.FirstOrDefault();
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

  #region Properties — IO Signals

  /// <summary>PC → Robot: Connect 명령 후 Start IO 전송됨</summary>
  public bool IoStartOn
  {
    get => _ioStartOn;
    private set => Set(ref _ioStartOn, value);
  }

  /// <summary>Robot → PC: Running IO On — 동작 준비 완료</summary>
  public bool IoRunning
  {
    get => _ioRunning;
    private set { if (Set(ref _ioRunning, value)) RaiseCanExecute(); }
  }

  /// <summary>Robot → PC: 로봇 이동 중</summary>
  public bool IoMoving
  {
    get => _ioMoving;
    private set => Set(ref _ioMoving, value);
  }

  /// <summary>Robot → PC: Go 명령 수신 확인</summary>
  public bool IoGoAck
  {
    get => _ioGoAck;
    private set => Set(ref _ioGoAck, value);
  }

  /// <summary>Robot → PC: 이동 완료</summary>
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

  private RobotPosition SelectedProgram =>
    _selectedProgramItem?.Position ?? RobotPosition.Ready;

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

      // 연결 끊기면 모든 IO 신호 Off
      if (wasConnected && !e.IsConnected)
        ResetAllSignals();

      if (e.IsError)
        LogStatus = $"[Error] {e.ErrorMessage}";

      // 이동 완료 감지: IsRunning 이 false 로 바뀔 때 IoMoving Off, IoComplete On
      if (IoMoving && !e.IsRunning)
        OnMoveCompleted();
    });
  }

  private void OnMoveCompleted()
  {
    IoMoving  = false;
    IoComplete = true;
    LogStatus = $"Complete: {CurrentPosition}";

    // Go Off → Complete Off (핸드셰이크 완료)
    Task.Delay(300).ContinueWith(_ =>
      Application.Current?.Dispatcher.Invoke(() =>
      {
        IoGoAck    = false;
        IoComplete = false;
        LogStatus  = $"Ready — {CurrentPosition}";
      }));
  }

  private void ResetAllSignals()
  {
    IoStartOn  = false;
    IoRunning  = false;
    IoMoving   = false;
    IoGoAck    = false;
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
    ResetAllSignals();
    _robot.Disconnect();
    SetLog("Disconnected");
  }

  #endregion

  #region Commands — IO Handshake

  private RelayCommand? cmd_InitStartIo;
  public ICommand Cmd_InitStartIo => cmd_InitStartIo ??=
    new RelayCommand(_ => InitStartIo(), _ => IsConnected && !IoRunning);

  private RelayCommand? cmd_Go;
  public ICommand Cmd_Go => cmd_Go ??=
    new RelayCommand(_ => Go(), _ => IsConnected && IoRunning && !IoMoving);

  private RelayCommand? cmd_Stop;
  public ICommand Cmd_Stop => cmd_Stop ??=
    new RelayCommand(_ => Stop(), _ => IsConnected);

  private RelayCommand? cmd_Reset;
  public ICommand Cmd_Reset => cmd_Reset ??=
    new RelayCommand(_ => Reset(), _ => IsConnected);

  private void InitStartIo()
  {
    // Start IO On → Robot Running IO On (동작 준비 완료)
    LogStatus = "Init: Start IO On...";
    IoStartOn = true;

    _ = Task.Run(async () =>
    {
      try
      {
        // GetState 로 연결 확인 후 Running 상태 활성화
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

  private void Go()
  {
    _activeCts?.Cancel();
    _activeCts?.Dispose();
    var cts = new CancellationTokenSource();
    _activeCts = cts;

    var target   = SelectedProgram;
    var velocity = _velocity;

    LogStatus = $"Go → {target} (vel={velocity}%)";

    _ = Task.Run(async () =>
    {
      try
      {
        // 속도 설정 (Write velocity word)
        await _robot.SetVelocityAsync(target, velocity, cts.Token)
          .ConfigureAwait(false);

        // Go On → Moving On, Go_Ack On
        var ok = await _robot.MoveAsync(target, cts.Token).ConfigureAwait(false);

        if (!ok)
        {
          SetLog($"Go rejected by robot — check interlock.");
          return;
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoGoAck  = true;
          IoMoving = true;
        });

        // WaitForPosition (Moving Off 대기)
        await _robot.WaitForPositionAsync(
          target,
          _core.Settings.RobotMoveTimeout,
          cts.Token).ConfigureAwait(false);

        // OnMoveCompleted 는 StateChanged 이벤트에서 자동 호출
      }
      catch (OperationCanceledException)
      {
        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoMoving = false;
          IoGoAck  = false;
          LogStatus = "Go cancelled";
        });
      }
      catch (Exception ex)
      {
        Application.Current?.Dispatcher.Invoke(() =>
        {
          IoMoving = false;
          IoGoAck  = false;
          LogStatus = $"[Error] {ex.Message}";
        });
      }
    });
  }

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
          IoMoving = false;
          IoGoAck  = false;
          LogStatus = "Stop sent";
        });
      }
      catch (Exception ex) { SetLog($"Stop error: {ex.Message}"); }
    });
  }

  private void Reset()
  {
    // 모든 플래그 Off → Running 만 On (다음 동작 대기)
    IoMoving   = false;
    IoGoAck    = false;
    IoComplete = false;
    IoRunning  = IoStartOn; // Start IO 가 켜져 있으면 Running 복원
    LogStatus  = "Reset — flags cleared, Running restored";
  }

  #endregion

  #region Helpers

  private void SetLog(string msg) =>
    Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);

  private void RaiseCanExecute()
  {
    cmd_Connect?.RaiseCanExecuteChanged();
    cmd_Disconnect?.RaiseCanExecuteChanged();
    cmd_InitStartIo?.RaiseCanExecuteChanged();
    cmd_Go?.RaiseCanExecuteChanged();
    cmd_Stop?.RaiseCanExecuteChanged();
    cmd_Reset?.RaiseCanExecuteChanged();
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
      Label = RobotPositionName.FromPosition(pos);
    }

    public override string ToString() => Label;
  }
}
