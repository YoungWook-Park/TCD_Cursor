using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

/// <summary>
/// IO 수동 제어 탭 ViewModel.
/// 인터락 로직은 시퀀스 내부에 있으며, 이 ViewModel은 시퀀스 키 호출만 담당.
/// </summary>
public sealed class Manual_IoViewModel : NotifyPropertyChangedBase
{
  #region Fields

  private readonly MainCore _core = MainCore.Instance;
  private CancellationTokenSource? _activeCts;
  private string _logStatus = "";

  #endregion

  #region Properties

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  #endregion

  #region Commands — Stage Vac

  public ICommand Cmd_LowStageVacOn  => MakeCmd(TcdSequenceKeys.Manual_Io_LowStageVacOn);
  public ICommand Cmd_LowStageVacOff => MakeCmd(TcdSequenceKeys.Manual_Io_LowStageVacOff);
  public ICommand Cmd_HighStageVacOn  => MakeCmd(TcdSequenceKeys.Manual_Io_HighStageVacOn);
  public ICommand Cmd_HighStageVacOff => MakeCmd(TcdSequenceKeys.Manual_Io_HighStageVacOff);

  #endregion

  #region Commands — Robot Grip Vac

  public ICommand Cmd_RobotGripVacOn  => MakeCmd(TcdSequenceKeys.Manual_Io_RobotGripVacOn);
  public ICommand Cmd_RobotGripVacOff => MakeCmd(TcdSequenceKeys.Manual_Io_RobotGripVacOff);

  #endregion

  #region Commands — Chamber Vac (인터락: Z Bond 위치)

  public ICommand Cmd_UpperChamberVacOn  => MakeCmd(TcdSequenceKeys.Manual_Io_UpperChamberVacOn);
  public ICommand Cmd_UpperChamberVacOff => MakeCmd(TcdSequenceKeys.Manual_Io_UpperChamberVacOff);
  public ICommand Cmd_LowerChamberVacOn  => MakeCmd(TcdSequenceKeys.Manual_Io_LowerChamberVacOn);
  public ICommand Cmd_LowerChamberVacOff => MakeCmd(TcdSequenceKeys.Manual_Io_LowerChamberVacOff);

  #endregion

  #region Commands — ESC (인터락: 챔버 진공 On)

  public ICommand Cmd_UpperEscEnable  => MakeCmd(TcdSequenceKeys.Manual_Io_UpperEscEnable);
  public ICommand Cmd_UpperEscDisable => MakeCmd(TcdSequenceKeys.Manual_Io_UpperEscDisable);
  public ICommand Cmd_LowerEscEnable  => MakeCmd(TcdSequenceKeys.Manual_Io_LowerEscEnable);
  public ICommand Cmd_LowerEscDisable => MakeCmd(TcdSequenceKeys.Manual_Io_LowerEscDisable);

  #endregion

  #region Helper

  private RelayCommand MakeCmd(string seqKey) =>
    new(_ => RunSequence(seqKey));

  private void RunSequence(string seqKey)
  {
    _activeCts?.Cancel();
    _activeCts?.Dispose();
    var cts = new CancellationTokenSource();
    _activeCts = cts;

    LogStatus = $"Running: {seqKey}";

    _ = Task.Run(async () =>
    {
      try
      {
        var result = await _core.Sequences
          .RunAsync(seqKey, _core.Simulation, null, cts.Token)
          .ConfigureAwait(false);

        SetLog(result.Status == SequenceStatus.Succeeded
          ? $"OK: {seqKey}"
          : $"FAIL: {result.Error ?? seqKey}");
      }
      catch (OperationCanceledException)
      {
        SetLog($"Cancelled: {seqKey}");
      }
      catch (Exception ex)
      {
        SetLog($"[Error] {ex.Message}");
      }
    });
  }

  private void SetLog(string msg) =>
    System.Windows.Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);

  #endregion
}
