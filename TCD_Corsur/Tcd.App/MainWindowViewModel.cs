using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class MainWindowViewModel : NotifyPropertyChangedBase
{
    #region Variable

    private readonly MainCore _core = MainCore.Instance;
    private readonly TcdSimulation _sim;
    private readonly DispatcherTimer _uiTimer;
    private readonly SequenceManager _seq;
    private CancellationTokenSource? _runCts;
    private bool _isRunning;
    private string _status = "Idle";
    private object _currentContent = null!;
    private string _stage1 = "(empty)";
    private string _stage2 = "(empty)";
    private string _upperChamber = "(empty)";
    private string _lowerChamber = "(empty)";
    private string _robot = "";
    private string _axes = "";
    private bool _stage1HasMaterial;
    private bool _stage2HasMaterial;
    private bool _upperChamberHasMaterial;
    private bool _lowerChamberHasMaterial;
    private double _zPosition;
    private RobotPosition _currentRobotPosition;
    private bool _isBonding;
    private bool _robotHasVacuum;

    #endregion

    #region Constructor

    public MainWindowViewModel()
    {
        _sim = _core.Simulation;
        _seq = _core.Sequences;

        Recipe = new RecipeViewModel();
        Manual = new ManualViewModel();
        CurrentContent = Main;

        _core.Recipes.CurrentChanged += (_, _) => Raise(nameof(CurrentRecipeName));

        _seq.Trace += (_, e) =>
            App.Current.Dispatcher.Invoke(() =>
            {
                var msg = e.Kind == SequenceTraceKind.Started
                    ? $"[{e.Timestamp:HH:mm:ss}] START {e.Key} ({e.DisplayName})"
                    : $"[{e.Timestamp:HH:mm:ss}] END   {e.Key} ({e.DisplayName}) => {e.Status}{(string.IsNullOrWhiteSpace(e.Error) ? "" : $" | {e.Error}")}";
                Alarms.Insert(0, msg);
            });

        _core.Alarms.AlarmRaised += (_, a) =>
            App.Current.Dispatcher.Invoke(() => Alarms.Insert(0, a.ToString()));

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _uiTimer.Tick += (_, _) => RefreshSnapshot();
        _uiTimer.Start();
    }

    #endregion

    #region Property

    public MainWindowViewModel Main => this;
    public RecipeViewModel Recipe { get; }
    public ManualViewModel Manual { get; }

    public object CurrentContent
    {
        get => _currentContent;
        private set => Set(ref _currentContent, value);
    }

    public ObservableCollection<string> Alarms { get; } = new();

    /// <summary>현재 선택된 레시피 이름 (메인 상단 표시용)</summary>
    public string CurrentRecipeName => _core.Recipes.Current?.Name ?? "(None)";

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!Set(ref _isRunning, value)) return;
            cmd_LoadStageCommand?.RaiseCanExecuteChanged();
            cmd_StartAutoCommand?.RaiseCanExecuteChanged();
            cmd_StopCommand?.RaiseCanExecuteChanged();
            cmd_UnloadProductCommand?.RaiseCanExecuteChanged();
            cmd_ClearCommand?.RaiseCanExecuteChanged();
        }
    }

    public string Status { get => _status; private set => Set(ref _status, value); }
    public string Stage1 { get => _stage1; private set => Set(ref _stage1, value); }
    public string Stage2 { get => _stage2; private set => Set(ref _stage2, value); }
    public string UpperChamber { get => _upperChamber; private set => Set(ref _upperChamber, value); }
    public string LowerChamber { get => _lowerChamber; private set => Set(ref _lowerChamber, value); }
    public string Robot { get => _robot; private set => Set(ref _robot, value); }
    public string Axes { get => _axes; private set => Set(ref _axes, value); }
    public bool Stage1HasMaterial { get => _stage1HasMaterial; private set => Set(ref _stage1HasMaterial, value); }
    public bool Stage2HasMaterial { get => _stage2HasMaterial; private set => Set(ref _stage2HasMaterial, value); }
    public bool UpperChamberHasMaterial { get => _upperChamberHasMaterial; private set => Set(ref _upperChamberHasMaterial, value); }
    public bool LowerChamberHasMaterial { get => _lowerChamberHasMaterial; private set => Set(ref _lowerChamberHasMaterial, value); }
    public double ZPosition { get => _zPosition; private set => Set(ref _zPosition, value); }
    public RobotPosition CurrentRobotPosition { get => _currentRobotPosition; private set => Set(ref _currentRobotPosition, value); }
    /// <summary>양쪽 ESC에 자재 부착 완료 후 합착 진행 중. 개략도 Z 이동 애니메이션용.</summary>
    public bool IsBonding { get => _isBonding; private set => Set(ref _isBonding, value); }
    /// <summary>로봇 진공 흡착 상태. 개략도 로봇 표시용.</summary>
    public bool RobotHasVacuum { get => _robotHasVacuum; private set => Set(ref _robotHasVacuum, value); }

    #endregion

    #region Function

    private void RefreshSnapshot()
    {
        var s1 = _sim.Materials.Get(MaterialLocation.Stage1);
        var s2 = _sim.Materials.Get(MaterialLocation.Stage2);
        var up = _sim.Materials.Get(MaterialLocation.UpperChamber);
        var low = _sim.Materials.Get(MaterialLocation.LowerChamber);

        Stage1 = s1?.Kind.ToString() ?? "(empty)";
        Stage2 = s2?.Kind.ToString() ?? "(empty)";
        UpperChamber = up?.Kind.ToString() ?? "(empty)";
        LowerChamber = low?.Kind.ToString() ?? "(empty)";

        Stage1HasMaterial = s1 != null;
        Stage2HasMaterial = s2 != null;
        UpperChamberHasMaterial = up != null;
        LowerChamberHasMaterial = low != null;

        ZPosition             = _sim.LowerMotion.Z.Position;
        CurrentRobotPosition  = _sim.Robot.CurrentPosition;
        RobotHasVacuum        = _sim.Robot.HasVacuum;
        IsBonding             = IsRunning && UpperChamberHasMaterial && LowerChamberHasMaterial;

        Robot = $"{_sim.Robot.CurrentPosition} | Vacuum={_sim.Robot.HasVacuum}";
        Axes  = $"U={_sim.LowerMotion.U.Position:0.0}, V={_sim.LowerMotion.V.Position:0.0}, W={_sim.LowerMotion.W.Position:0.0}, Z={_sim.LowerMotion.Z.Position:0.0}";
    }

    #endregion

    #region UI Function

    private BiRelayCommand? cmd_LoadStageCommand;
    public ICommand Cmd_LoadStageCommand =>
        cmd_LoadStageCommand ??= new BiRelayCommand(PerformCmd_LoadStage, _ => !IsRunning);

    private void PerformCmd_LoadStage(object? commandParameter)
    {
        try
        {
            _sim.LoadStage(MaterialKind.UpperFilm, MaterialKind.LowerFilm);
            RefreshSnapshot();
        }
        catch (Exception ex) { Alarms.Insert(0, $"[VM] {ex.Message}"); }
        finally { }
    }

    private BiRelayCommand? cmd_StartAutoCommand;
    public ICommand Cmd_StartAutoCommand =>
        cmd_StartAutoCommand ??= new BiRelayCommand(PerformCmd_StartAuto, _ => !IsRunning);

    private void PerformCmd_StartAuto(object? commandParameter)
    {
        try
        {
            IsRunning = true;
            Status = "Running...";
            _runCts = new CancellationTokenSource();
            _sim.BindStopToken(_runCts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _seq.RunAsync(TcdSequenceKeys.AUTO_Run, _sim, null, _runCts.Token).ConfigureAwait(false);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Status = result.Status == SequenceStatus.Succeeded ? "Done" : $"Stopped/Failed: {result.Status}";
                        IsRunning = false;
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Alarms.Insert(0, $"[VM] Exception: {ex.Message}");
                        Status = "Failed";
                        IsRunning = false;
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Alarms.Insert(0, $"[VM] {ex.Message}");
            Status = "Failed";
            IsRunning = false;
        }
        finally { }
    }

    private BiRelayCommand? cmd_StopCommand;
    public ICommand Cmd_StopCommand =>
        cmd_StopCommand ??= new BiRelayCommand(PerformCmd_Stop, _ => IsRunning);

    private void PerformCmd_Stop(object? commandParameter)
    {
        try { _runCts?.Cancel(); }
        catch (Exception ex) { Alarms.Insert(0, $"[VM] {ex.Message}"); }
        finally { }
    }

    private BiRelayCommand? cmd_UnloadProductCommand;
    public ICommand Cmd_UnloadProductCommand =>
        cmd_UnloadProductCommand ??= new BiRelayCommand(PerformCmd_UnloadProduct, _ => !IsRunning);

    private void PerformCmd_UnloadProduct(object? commandParameter)
    {
        try
        {
            IsRunning = true;
            Status = "Unload product...";
            _runCts = new CancellationTokenSource();
            _sim.BindStopToken(_runCts.Token);
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _seq.RunAsync(TcdSequenceKeys.SEMI_UnloadProductToStage2, _sim, null, _runCts.Token).ConfigureAwait(false);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Status = result.Status == SequenceStatus.Succeeded ? "Unload done" : $"Unload: {result.Status}";
                        IsRunning = false;
                        RefreshSnapshot();
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Alarms.Insert(0, $"[VM] Unload: {ex.Message}");
                        Status = "Unload failed";
                        IsRunning = false;
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Alarms.Insert(0, $"[VM] {ex.Message}");
            Status = "Failed";
            IsRunning = false;
        }
        finally { }
    }

    private BiRelayCommand? cmd_ClearCommand;
    public ICommand Cmd_ClearCommand =>
        cmd_ClearCommand ??= new BiRelayCommand(PerformCmd_Clear, _ => !IsRunning);

    private void PerformCmd_Clear(object? commandParameter)
    {
        try
        {
            _sim.Reset();
            Alarms.Clear();
            RefreshSnapshot();
        }
        catch (Exception ex) { Alarms.Insert(0, $"[VM] {ex.Message}"); }
        finally { }
    }

    private BiRelayCommand? cmd_ExitCommand;
    public ICommand Cmd_ExitCommand =>
        cmd_ExitCommand ??= new BiRelayCommand(PerformCmd_Exit, _ => true);

    private void PerformCmd_Exit(object? commandParameter)
    {
        App.Current.Shutdown();
    }

    private BiRelayCommand? cmd_ShowMainPage;
    public ICommand Cmd_ShowMainPage => cmd_ShowMainPage ??= new BiRelayCommand(_ => CurrentContent = Main);

    private BiRelayCommand? cmd_ShowRecipePage;
    public ICommand Cmd_ShowRecipePage => cmd_ShowRecipePage ??= new BiRelayCommand(_ => CurrentContent = Recipe);

    private BiRelayCommand? cmd_ShowManualPage;
    public ICommand Cmd_ShowManualPage => cmd_ShowManualPage ??= new BiRelayCommand(_ => CurrentContent = Manual);

    #endregion
}
