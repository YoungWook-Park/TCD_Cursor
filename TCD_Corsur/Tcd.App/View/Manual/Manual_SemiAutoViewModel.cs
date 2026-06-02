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
/// 반자동 Pick-Place 탭 ViewModel.
/// ViewModel은 시퀀스 키 호출만 담당 — 인터락/IO는 시퀀스 내부 처리.
/// </summary>
public sealed class Manual_SemiAutoViewModel : NotifyPropertyChangedBase
{
  private readonly MainCore _core = MainCore.Instance;
  private CancellationTokenSource? _activeCts;
  private string _logStatus = "";

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  // ── Load: Stage → Chamber ────────────────────────────────────────────

  public ICommand Cmd_UpperStagePick_UpperChamberPlace =>
    MakeCmd(TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace);

  public ICommand Cmd_LowerStagePick_LowerChamberPlace =>
    MakeCmd(TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace);

  // ── Unload: Chamber → Stage ──────────────────────────────────────────

  public ICommand Cmd_UpperChamberPick_UpperStagePlace =>
    MakeCmd(TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace);

  public ICommand Cmd_LowerChamberPick_LowerStagePlace =>
    MakeCmd(TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace);

  // ── Stop ─────────────────────────────────────────────────────────────

  public ICommand Cmd_Stop => new RelayCommand(_ =>
  {
    _activeCts?.Cancel();
    LogStatus = "Stop requested";
  });

  // ── Helper ───────────────────────────────────────────────────────────

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
      catch (OperationCanceledException) { SetLog("Stopped"); }
      catch (Exception ex) { SetLog($"[Error] {ex.Message}"); }
    });
  }

  private void SetLog(string msg) =>
    System.Windows.Application.Current?.Dispatcher.Invoke(() => LogStatus = msg);
}
