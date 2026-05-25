using System;
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
/// 로봇 수동 제어 ViewModel.
/// RobotTcpClient(IRobotDevice) 를 통해 로봇 시뮬레이터와 통신한다.
///
/// 실행 흐름:
///   연결 → SetVelocity(pos, pct) → Move(pos) → WaitForPosition(pos, timeout)
///
/// 인터락:
///   IsConnected=false  → Move 불가
///   IsRunning=true     → Move 불가
///   IsHome/IsReady     → 안전 위치 아니면 Ready 이동만 허용 (서버 측도 동일 체크)
/// </summary>
public sealed class Manual_RobotViewModel : NotifyPropertyChangedBase
{
    #region Variable

    private readonly MainCore    _core    = MainCore.Instance;
    private readonly IRobotDevice _robot;
    private CancellationTokenSource? _activeCts;

    private string _logStatus = "";

    #endregion

    #region Constructor

    public Manual_RobotViewModel()
    {
        _robot = _core.RobotDevice;
        _robot.StateChanged += OnRobotStateChanged;
    }

    #endregion

    #region Robot State Properties (IRobotDevice 미러링)

    public bool IsConnected    => _robot.IsConnected;
    public bool IsRunning      => _robot.IsRunning;
    public bool IsHome         => _robot.IsHome;
    public bool IsError        => _robot.IsError;
    public RobotPosition CurrentPosition => _robot.CurrentPosition;
    public string? ErrorMessage => _robot.ErrorMessage;

    #endregion

    #region Log

    public string LogStatus
    {
        get => _logStatus;
        private set => Set(ref _logStatus, value);
    }

    #endregion

    #region State Change

    private void OnRobotStateChanged(object? sender, RobotDeviceStateArgs e)
    {
        // 백그라운드 스레드에서 발생 → UI 스레드 마샬링
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Raise(nameof(IsConnected));
            Raise(nameof(IsRunning));
            Raise(nameof(IsHome));
            Raise(nameof(IsError));
            Raise(nameof(CurrentPosition));
            Raise(nameof(ErrorMessage));
            RaiseMoveCommandsCanExecute();

            if (e.IsError)
                LogStatus = $"[Error] {e.ErrorMessage}";
        });
    }

    private void RaiseMoveCommandsCanExecute()
    {
        cmd_Stop?.RaiseCanExecuteChanged();
        cmd_MoveHome?.RaiseCanExecuteChanged();
        cmd_MoveReady?.RaiseCanExecuteChanged();
        cmd_MoveS1Wait?.RaiseCanExecuteChanged();
        cmd_MoveS1Pick?.RaiseCanExecuteChanged();
        cmd_MoveS2Wait?.RaiseCanExecuteChanged();
        cmd_MoveS2Pick?.RaiseCanExecuteChanged();
        cmd_MoveUcWait?.RaiseCanExecuteChanged();
        cmd_MoveUcPick?.RaiseCanExecuteChanged();
        cmd_MoveLcWait?.RaiseCanExecuteChanged();
        cmd_MoveLcPick?.RaiseCanExecuteChanged();
        cmd_MovePeel?.RaiseCanExecuteChanged();
    }

    #endregion

    #region Move Helper

    /// <summary>
    /// 현재 레시피에서 포지션 속도를 읽어 반환.
    /// 레시피 미설정 시 RobotVelocityDefault 폴백.
    /// </summary>
    private int GetVelocity(RobotPosition pos)
    {
        var key    = RobotPositionName.FromPosition(pos);
        var recipe = _core.Recipes.Current;
        if (recipe?.RobotVelocity?.TryGetValue(key, out var v) == true)
            return Math.Clamp(v, 1, 100);
        return RobotVelocityDefault.ForPosition(pos);
    }

    /// <summary>
    /// SetVelocity → Move → 완료 대기 공통 처리.
    /// _activeCts 로 이전 대기 취소 후 새 작업 시작.
    /// </summary>
    private void ExecuteMove(
        RobotPosition target, int velocityPct, string displayName)
    {
        _activeCts?.Cancel();
        _activeCts?.Dispose();
        var cts = new CancellationTokenSource();
        _activeCts = cts;

        LogStatus = $"Moving → {displayName} (vel={velocityPct}%)";

        _ = Task.Run(async () =>
        {
            try
            {
                await _robot.SetVelocityAsync(target, velocityPct, cts.Token)
                             .ConfigureAwait(false);
                var ok = await _robot.MoveAsync(target, cts.Token)
                                     .ConfigureAwait(false);
                if (!ok)
                {
                    SetLog($"Move rejected by server — check interlock.");
                    return;
                }

                await _robot.WaitForPositionAsync(
                        target,
                        _core.Settings.RobotMoveTimeout,
                        cts.Token)
                    .ConfigureAwait(false);

                SetLog($"Arrived: {displayName}");
            }
            catch (OperationCanceledException)
            {
                SetLog($"Cancelled: {displayName}");
            }
            catch (Exception ex)
            {
                SetLog($"[Error] {ex.Message}");
            }
        });
    }

    private void SetLog(string msg) =>
        Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);

    #endregion

    #region Commands — Stop

    private RelayCommand? cmd_Stop;
    public ICommand Cmd_Stop => cmd_Stop ??=
        new RelayCommand(
            _ => StopAsync(),
            _ => IsConnected);

    private void StopAsync()
    {
        _activeCts?.Cancel();
        _ = _robot.StopAsync();
        LogStatus = "Stop sent";
    }

    #endregion

    #region Commands — Move (11개 포지션)

    // ── 안전 위치 ──────────────────────────────────────────────────────────
    private RelayCommand? cmd_MoveHome;
    public ICommand Cmd_MoveHome => cmd_MoveHome ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.Home,
                             GetVelocity(RobotPosition.Home),
                             RobotPositionName.Home),
            _ => IsConnected && !IsRunning);

    private RelayCommand? cmd_MoveReady;
    public ICommand Cmd_MoveReady => cmd_MoveReady ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.Ready,
                             GetVelocity(RobotPosition.Ready),
                             RobotPositionName.Ready),
            _ => IsConnected && !IsRunning);

    // ── S1 (CGO / 상부 필름) ───────────────────────────────────────────────
    private RelayCommand? cmd_MoveS1Wait;
    public ICommand Cmd_MoveS1Wait => cmd_MoveS1Wait ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.S1_PickupWait,
                             GetVelocity(RobotPosition.S1_PickupWait),
                             RobotPositionName.S1_PickupWait),
            _ => IsConnected && !IsRunning);

    private RelayCommand? cmd_MoveS1Pick;
    public ICommand Cmd_MoveS1Pick => cmd_MoveS1Pick ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.S1_Pick,
                             GetVelocity(RobotPosition.S1_Pick),
                             RobotPositionName.S1_Pick),
            _ => IsConnected && !IsRunning);

    // ── S2 / LowStage (OCA / 하부 필름) ───────────────────────────────────
    private RelayCommand? cmd_MoveS2Wait;
    public ICommand Cmd_MoveS2Wait => cmd_MoveS2Wait ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.S2_PickupWait,
                             GetVelocity(RobotPosition.S2_PickupWait),
                             RobotPositionName.S2_PickupWait),
            _ => IsConnected && !IsRunning);

    private RelayCommand? cmd_MoveS2Pick;
    public ICommand Cmd_MoveS2Pick => cmd_MoveS2Pick ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.S2_Pick,
                             GetVelocity(RobotPosition.S2_Pick),
                             RobotPositionName.S2_Pick),
            _ => IsConnected && !IsRunning);

    // ── Upper Chamber ──────────────────────────────────────────────────────
    private RelayCommand? cmd_MoveUcWait;
    public ICommand Cmd_MoveUcWait => cmd_MoveUcWait ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.UpperChamber_PickupWait,
                             GetVelocity(RobotPosition.UpperChamber_PickupWait),
                             RobotPositionName.UpperChamber_PickupWait),
            _ => IsConnected && !IsRunning);

    private RelayCommand? cmd_MoveUcPick;
    public ICommand Cmd_MoveUcPick => cmd_MoveUcPick ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.UpperChamber_Pick,
                             GetVelocity(RobotPosition.UpperChamber_Pick),
                             RobotPositionName.UpperChamber_Pick),
            _ => IsConnected && !IsRunning);

    // ── Lower Chamber ──────────────────────────────────────────────────────
    private RelayCommand? cmd_MoveLcWait;
    public ICommand Cmd_MoveLcWait => cmd_MoveLcWait ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.LowerChamber_PickupWait,
                             GetVelocity(RobotPosition.LowerChamber_PickupWait),
                             RobotPositionName.LowerChamber_PickupWait),
            _ => IsConnected && !IsRunning);

    private RelayCommand? cmd_MoveLcPick;
    public ICommand Cmd_MoveLcPick => cmd_MoveLcPick ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.LowerChamber_Pick,
                             GetVelocity(RobotPosition.LowerChamber_Pick),
                             RobotPositionName.LowerChamber_Pick),
            _ => IsConnected && !IsRunning);

    // ── Peel ───────────────────────────────────────────────────────────────
    private RelayCommand? cmd_MovePeel;
    public ICommand Cmd_MovePeel => cmd_MovePeel ??=
        new RelayCommand(
            _ => ExecuteMove(RobotPosition.Peel,
                             GetVelocity(RobotPosition.Peel),
                             RobotPositionName.Peel),
            _ => IsConnected && !IsRunning);

    #endregion
}
