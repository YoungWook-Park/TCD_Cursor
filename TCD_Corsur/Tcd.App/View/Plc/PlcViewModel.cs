using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Devices;
using Tcd.App.Mvvm;
using Tcd.Devices;

namespace Tcd.App;

/// <summary>
/// PLC 연결 / IO맵 모니터링 ViewModel.
///
/// 흐름:
///   Connect → StartMonitor → SnapshotUpdated 이벤트(100ms) → UI 갱신
///
/// DO 쓰기:
///   토글 커맨드 → WriteBitAsync(fire-and-forget) → 다음 스냅샷에서 반영 확인
/// </summary>
public sealed class PlcViewModel : NotifyPropertyChangedBase
{
  #region Variable

  private readonly MainCore     _core = MainCore.Instance;
  private readonly PlcTcpClient _plc;

  private string _host;
  private int    _port;
  private bool   _isConnecting;
  private bool   _isMonitoring;
  private string _logStatus = "";

  // ── DI (PLC → WPF) ──────────────────────────────────────────────────
  private bool _di_EStop_OK;
  private bool _di_DoorClosed;
  private bool _di_LowStageVac;
  private bool _di_HighStageVac;
  private bool _di_RobotGripVac;
  private bool _di_UpperChamberVac;
  private bool _di_LowerChamberAtReady;
  private bool _di_UpperChamberAtReady;
  private bool _di_LowerChamberAtBond;
  private bool _di_UpperChamberAtBond;
  private bool _di_MaterialLowStage;
  private bool _di_MaterialHighStage;
  private bool _di_AtAtmospheric;

  // ── DO (WPF → PLC, 스냅샷에서 읽어 미러링) ─────────────────────────
  private bool _do_VacPump;
  private bool _do_LowStageVac;
  private bool _do_HighStageVac;
  private bool _do_RobotGripVac;
  private bool _do_UpperChamberVac;
  private bool _do_EscEnable;
  private bool _do_ChamberMoveToBond;
  private bool _do_LaminationActive;
  private bool _do_VentValveOpen;

  // ── AI (PLC → WPF, 스케일 변환 후 표시) ───────────────────────────
  private double _chamberPressure_kPa;  // W0 / 100
  private double _loadcell_N;           // W2 / 10
  private double _chamberVacuum_kPa;    // W4 / 10

  #endregion

  #region Constructor

  public PlcViewModel()
  {
    _plc  = _core.PlcDevice;
    _host = _core.Devices.PlcHost;
    _port = _core.Devices.PlcPort;

    _plc.SnapshotUpdated += OnSnapshotUpdated;
  }

  #endregion

  #region Connection Properties

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
    private set
    {
      if (!Set(ref _isConnecting, value)) return;
      RaiseAllCanExecute();
    }
  }

  public bool IsConnected => _plc.IsConnected;

  public bool IsMonitoring
  {
    get => _isMonitoring;
    private set
    {
      if (!Set(ref _isMonitoring, value)) return;
      RaiseAllCanExecute();
    }
  }

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  #endregion

  #region DI Properties

  public bool DI_EStop_OK
  {
    get => _di_EStop_OK;
    private set => Set(ref _di_EStop_OK, value);
  }

  public bool DI_DoorClosed
  {
    get => _di_DoorClosed;
    private set => Set(ref _di_DoorClosed, value);
  }

  public bool DI_LowStageVac
  {
    get => _di_LowStageVac;
    private set => Set(ref _di_LowStageVac, value);
  }

  public bool DI_HighStageVac
  {
    get => _di_HighStageVac;
    private set => Set(ref _di_HighStageVac, value);
  }

  public bool DI_RobotGripVac
  {
    get => _di_RobotGripVac;
    private set => Set(ref _di_RobotGripVac, value);
  }

  public bool DI_UpperChamberVac
  {
    get => _di_UpperChamberVac;
    private set => Set(ref _di_UpperChamberVac, value);
  }

  public bool DI_LowerChamberAtReady
  {
    get => _di_LowerChamberAtReady;
    private set => Set(ref _di_LowerChamberAtReady, value);
  }

  public bool DI_UpperChamberAtReady
  {
    get => _di_UpperChamberAtReady;
    private set => Set(ref _di_UpperChamberAtReady, value);
  }

  public bool DI_LowerChamberAtBond
  {
    get => _di_LowerChamberAtBond;
    private set => Set(ref _di_LowerChamberAtBond, value);
  }

  public bool DI_UpperChamberAtBond
  {
    get => _di_UpperChamberAtBond;
    private set => Set(ref _di_UpperChamberAtBond, value);
  }

  public bool DI_MaterialLowStage
  {
    get => _di_MaterialLowStage;
    private set => Set(ref _di_MaterialLowStage, value);
  }

  public bool DI_MaterialHighStage
  {
    get => _di_MaterialHighStage;
    private set => Set(ref _di_MaterialHighStage, value);
  }

  public bool DI_AtAtmospheric
  {
    get => _di_AtAtmospheric;
    private set => Set(ref _di_AtAtmospheric, value);
  }

  #endregion

  #region DO Properties

  public bool DO_VacPump
  {
    get => _do_VacPump;
    private set => Set(ref _do_VacPump, value);
  }

  public bool DO_LowStageVac
  {
    get => _do_LowStageVac;
    private set => Set(ref _do_LowStageVac, value);
  }

  public bool DO_HighStageVac
  {
    get => _do_HighStageVac;
    private set => Set(ref _do_HighStageVac, value);
  }

  public bool DO_RobotGripVac
  {
    get => _do_RobotGripVac;
    private set => Set(ref _do_RobotGripVac, value);
  }

  public bool DO_UpperChamberVac
  {
    get => _do_UpperChamberVac;
    private set => Set(ref _do_UpperChamberVac, value);
  }

  public bool DO_EscEnable
  {
    get => _do_EscEnable;
    private set => Set(ref _do_EscEnable, value);
  }

  public bool DO_ChamberMoveToBond
  {
    get => _do_ChamberMoveToBond;
    private set => Set(ref _do_ChamberMoveToBond, value);
  }

  public bool DO_LaminationActive
  {
    get => _do_LaminationActive;
    private set => Set(ref _do_LaminationActive, value);
  }

  public bool DO_VentValveOpen
  {
    get => _do_VentValveOpen;
    private set => Set(ref _do_VentValveOpen, value);
  }

  #endregion

  #region AI Properties

  /// <summary>챔버 압력 (kPa). W0 / 100. 음수 = 진공.</summary>
  public double ChamberPressure_kPa
  {
    get => _chamberPressure_kPa;
    private set => Set(ref _chamberPressure_kPa, value);
  }

  /// <summary>로드셀 (N). W2 / 10.</summary>
  public double Loadcell_N
  {
    get => _loadcell_N;
    private set => Set(ref _loadcell_N, value);
  }

  /// <summary>챔버 진공도 (kPa). W4 / 10.</summary>
  public double ChamberVacuum_kPa
  {
    get => _chamberVacuum_kPa;
    private set => Set(ref _chamberVacuum_kPa, value);
  }

  #endregion

  #region Snapshot Handler

  private void OnSnapshotUpdated(object? sender, PlcSnapshotArgs e)
  {
    Application.Current?.Dispatcher.Invoke(() =>
    {
      // ── DI bits ──────────────────────────────────────────────────────
      DI_EStop_OK            = GetBit(e.Bits, (int)DiBit.EStop_OK);
      DI_DoorClosed          = GetBit(e.Bits, (int)DiBit.DoorClosed);
      DI_LowStageVac         = GetBit(e.Bits, (int)DiBit.LowStageVac);
      DI_HighStageVac        = GetBit(e.Bits, (int)DiBit.HighStageVac);
      DI_RobotGripVac        = GetBit(e.Bits, (int)DiBit.RobotGripVac);
      DI_UpperChamberVac     = GetBit(e.Bits, (int)DiBit.UpperChamberVac);
      DI_LowerChamberAtReady = GetBit(e.Bits, (int)DiBit.LowerChamberAtReady);
      DI_UpperChamberAtReady = GetBit(e.Bits, (int)DiBit.UpperChamberAtReady);
      DI_LowerChamberAtBond  = GetBit(e.Bits, (int)DiBit.LowerChamberAtBond);
      DI_UpperChamberAtBond  = GetBit(e.Bits, (int)DiBit.UpperChamberAtBond);
      DI_MaterialLowStage    = GetBit(e.Bits, (int)DiBit.MaterialLowStage);
      DI_MaterialHighStage   = GetBit(e.Bits, (int)DiBit.MaterialHighStage);
      DI_AtAtmospheric       = GetBit(e.Bits, (int)DiBit.AtAtmospheric);

      // ── DO bits (스냅샷에서 현재 출력 상태 미러링) ──────────────────
      DO_VacPump          = GetBit(e.Bits, (int)DoBit.VacPumpRequest);
      DO_LowStageVac      = GetBit(e.Bits, (int)DoBit.LowStageVacOn);
      DO_HighStageVac     = GetBit(e.Bits, (int)DoBit.HighStageVacOn);
      DO_RobotGripVac     = GetBit(e.Bits, (int)DoBit.RobotGripVacOn);
      DO_UpperChamberVac  = GetBit(e.Bits, (int)DoBit.UpperChamberVacOn);
      DO_EscEnable        = GetBit(e.Bits, (int)DoBit.EscEnable);
      DO_ChamberMoveToBond= GetBit(e.Bits, (int)DoBit.ChamberMoveToBond);
      DO_LaminationActive = GetBit(e.Bits, (int)DoBit.LaminationActive);
      DO_VentValveOpen    = GetBit(e.Bits, (int)DoBit.VentValveOpen);

      // ── AI words ─────────────────────────────────────────────────────
      if (e.Words.Length > (int)AiWord.ChamberPressure)
        ChamberPressure_kPa = e.Words[(int)AiWord.ChamberPressure] / 100.0;
      if (e.Words.Length > (int)AiWord.Loadcell)
        Loadcell_N = e.Words[(int)AiWord.Loadcell] / 10.0;
      if (e.Words.Length > (int)AiWord.ChamberVacuum)
        ChamberVacuum_kPa = e.Words[(int)AiWord.ChamberVacuum] / 10.0;
    });
  }

  private static bool GetBit(byte[] bits, int addr)
  {
    var byteIdx = addr / 8;
    var bitIdx  = addr % 8;
    if (byteIdx >= bits.Length) return false;
    return (bits[byteIdx] & (1 << bitIdx)) != 0;
  }

  #endregion

  #region Commands — Connection

  private RelayCommand? cmd_Connect;
  public ICommand Cmd_Connect => cmd_Connect ??=
    new RelayCommand(
      _ => ConnectAsync(),
      _ => !IsConnected && !IsConnecting);

  private RelayCommand? cmd_Disconnect;
  public ICommand Cmd_Disconnect => cmd_Disconnect ??=
    new RelayCommand(
      _ => Disconnect(),
      _ => IsConnected);

  private void ConnectAsync()
  {
    IsConnecting = true;
    LogStatus = $"Connecting to {Host}:{Port}...";
    _ = Task.Run(async () =>
    {
      try
      {
        await _plc.ConnectAsync(Host, Port).ConfigureAwait(false);
        SetLog($"Connected to {Host}:{Port}");
      }
      catch (Exception ex)
      {
        SetLog($"Connect failed: {ex.Message}");
      }
      finally
      {
        Application.Current?.Dispatcher.Invoke(() =>
        {
          IsConnecting = false;
          Raise(nameof(IsConnected));
          RaiseAllCanExecute();
        });
      }
    });
  }

  private void Disconnect()
  {
    if (IsMonitoring) StopMonitor();
    _plc.Disconnect();
    Application.Current?.Dispatcher.Invoke(() =>
    {
      Raise(nameof(IsConnected));
      RaiseAllCanExecute();
      LogStatus = "Disconnected";
    });
  }

  #endregion

  #region Commands — Monitoring

  private RelayCommand? cmd_StartMonitor;
  public ICommand Cmd_StartMonitor => cmd_StartMonitor ??=
    new RelayCommand(
      _ => StartMonitor(),
      _ => IsConnected && !IsMonitoring);

  private RelayCommand? cmd_StopMonitor;
  public ICommand Cmd_StopMonitor => cmd_StopMonitor ??=
    new RelayCommand(
      _ => StopMonitor(),
      _ => IsMonitoring);

  private void StartMonitor()
  {
    _plc.StartMonitoring(PlcDefine.MonitorInterval);
    IsMonitoring = true;
    LogStatus = $"Monitoring started ({PlcDefine.MonitorInterval.TotalMilliseconds}ms)";
  }

  private void StopMonitor()
  {
    _plc.StopMonitoring();
    IsMonitoring = false;
    LogStatus = "Monitoring stopped";
  }

  #endregion

  #region Commands — DO Write

  private RelayCommand? cmd_ToggleVacPump;
  public ICommand Cmd_ToggleVacPump => cmd_ToggleVacPump ??=
    new RelayCommand(
      _ => WriteDo(DoBit.VacPumpRequest, !DO_VacPump, "VacPump"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleLowStageVac;
  public ICommand Cmd_ToggleLowStageVac => cmd_ToggleLowStageVac ??=
    new RelayCommand(
      _ => WriteDo(DoBit.LowStageVacOn, !DO_LowStageVac, "LowStageVac"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleHighStageVac;
  public ICommand Cmd_ToggleHighStageVac => cmd_ToggleHighStageVac ??=
    new RelayCommand(
      _ => WriteDo(DoBit.HighStageVacOn, !DO_HighStageVac, "HighStageVac"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleRobotGripVac;
  public ICommand Cmd_ToggleRobotGripVac => cmd_ToggleRobotGripVac ??=
    new RelayCommand(
      _ => WriteDo(DoBit.RobotGripVacOn, !DO_RobotGripVac, "RobotGripVac"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleUpperChamberVac;
  public ICommand Cmd_ToggleUpperChamberVac => cmd_ToggleUpperChamberVac ??=
    new RelayCommand(
      _ => WriteDo(DoBit.UpperChamberVacOn, !DO_UpperChamberVac, "UpperChamberVac"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleEscEnable;
  public ICommand Cmd_ToggleEscEnable => cmd_ToggleEscEnable ??=
    new RelayCommand(
      _ => WriteDo(DoBit.EscEnable, !DO_EscEnable, "ESC Enable"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleChamberBond;
  public ICommand Cmd_ToggleChamberBond => cmd_ToggleChamberBond ??=
    new RelayCommand(
      _ => WriteDo(DoBit.ChamberMoveToBond, !DO_ChamberMoveToBond, "ChamberBond"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleLamination;
  public ICommand Cmd_ToggleLamination => cmd_ToggleLamination ??=
    new RelayCommand(
      _ => WriteDo(DoBit.LaminationActive, !DO_LaminationActive, "Lamination"),
      _ => IsConnected);

  private RelayCommand? cmd_ToggleVentValve;
  public ICommand Cmd_ToggleVentValve => cmd_ToggleVentValve ??=
    new RelayCommand(
      _ => WriteDo(DoBit.VentValveOpen, !DO_VentValveOpen, "VentValve"),
      _ => IsConnected);

  /// <summary>DO 비트 쓰기 (fire-and-forget). 결과는 다음 스냅샷에서 반영.</summary>
  private void WriteDo(DoBit addr, bool value, string name)
  {
    _ = Task.Run(async () =>
    {
      try
      {
        await _plc.WriteBitAsync(addr, value, CancellationToken.None)
                  .ConfigureAwait(false);
        SetLog($"DO {name} → {(value ? "ON" : "OFF")}");
      }
      catch (Exception ex)
      {
        SetLog($"[DO Error] {name}: {ex.Message}");
      }
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
    cmd_StartMonitor?.RaiseCanExecuteChanged();
    cmd_StopMonitor?.RaiseCanExecuteChanged();
    cmd_ToggleVacPump?.RaiseCanExecuteChanged();
    cmd_ToggleLowStageVac?.RaiseCanExecuteChanged();
    cmd_ToggleHighStageVac?.RaiseCanExecuteChanged();
    cmd_ToggleRobotGripVac?.RaiseCanExecuteChanged();
    cmd_ToggleUpperChamberVac?.RaiseCanExecuteChanged();
    cmd_ToggleEscEnable?.RaiseCanExecuteChanged();
    cmd_ToggleChamberBond?.RaiseCanExecuteChanged();
    cmd_ToggleLamination?.RaiseCanExecuteChanged();
    cmd_ToggleVentValve?.RaiseCanExecuteChanged();
  }

  #endregion
}
