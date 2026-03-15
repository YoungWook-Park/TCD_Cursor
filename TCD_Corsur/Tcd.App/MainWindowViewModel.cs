using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Tcd.App.Core;
using Tcd.App.Manual;
using Tcd.App.Mvvm;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class MainWindowViewModel : NotifyPropertyChangedBase
{
    private readonly MainCore _core = MainCore.Instance;
    private readonly TcdSimulation _sim;
    private readonly DispatcherTimer _uiTimer;
    private readonly SequenceManager _seq;
    private CancellationTokenSource? _runCts;
    private bool _isRunning;
    private string _status = "Idle";

    public MainWindowViewModel()
    {
        _sim = _core.Simulation;
        _seq = _core.Sequences;

        Recipe = new RecipeViewModel();
        Manual = new ManualViewModel();

        // 초기 메인 화면
        CurrentContent = Main;

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

    // Tab view-models (콘텐츠 전환용)
    public MainWindowViewModel Main => this;
    public RecipeViewModel Recipe { get; }
    public ManualViewModel Manual { get; }

    private object _currentContent = null!;
    public object CurrentContent
    {
        get => _currentContent;
        private set => Set(ref _currentContent, value);
    }

    // -------------------------------------------------------------------------------------------
    // Commands (property style, as requested)
    private BiRelayCommand? cmd_LoadStageCommand;
    public ICommand Cmd_LoadStageCommand
    {
        get
        {
            if (cmd_LoadStageCommand == null)
                cmd_LoadStageCommand = new BiRelayCommand(Cmd_LoadStage, _ => !IsRunning);
            return cmd_LoadStageCommand;
        }
    }

    private BiRelayCommand? cmd_StartAutoCommand;
    public ICommand Cmd_StartAutoCommand
    {
        get
        {
            if (cmd_StartAutoCommand == null)
                cmd_StartAutoCommand = new BiRelayCommand(Cmd_StartAuto, _ => !IsRunning);
            return cmd_StartAutoCommand;
        }
    }

    private BiRelayCommand? cmd_StopCommand;
    public ICommand Cmd_StopCommand
    {
        get
        {
            if (cmd_StopCommand == null)
                cmd_StopCommand = new BiRelayCommand(Cmd_Stop, _ => IsRunning);
            return cmd_StopCommand;
        }
    }

    private BiRelayCommand? cmd_ClearCommand;
    public ICommand Cmd_ClearCommand
    {
        get
        {
            if (cmd_ClearCommand == null)
                cmd_ClearCommand = new BiRelayCommand(Cmd_Clear, _ => !IsRunning);
            return cmd_ClearCommand;
        }
    }

    private BiRelayCommand? cmd_ExitCommand;
    public ICommand Cmd_ExitCommand
    {
        get
        {
            if (cmd_ExitCommand == null)
                cmd_ExitCommand = new BiRelayCommand(Cmd_Exit, _ => true);
            return cmd_ExitCommand;
        }
    }

    // 하단 네비게이션용 명령
    private BiRelayCommand? cmd_ShowMainPage;
    public ICommand Cmd_ShowMainPage
        => cmd_ShowMainPage ??= new BiRelayCommand(_ => CurrentContent = Main);

    private BiRelayCommand? cmd_ShowRecipePage;
    public ICommand Cmd_ShowRecipePage
        => cmd_ShowRecipePage ??= new BiRelayCommand(_ => CurrentContent = Recipe);

    private BiRelayCommand? cmd_ShowManualPage;
    public ICommand Cmd_ShowManualPage
        => cmd_ShowManualPage ??= new BiRelayCommand(_ => CurrentContent = Manual);

    public ObservableCollection<string> Alarms { get; } = new();

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!Set(ref _isRunning, value)) return;
            cmd_LoadStageCommand?.RaiseCanExecuteChanged();
            cmd_StartAutoCommand?.RaiseCanExecuteChanged();
            cmd_StopCommand?.RaiseCanExecuteChanged();
            cmd_ClearCommand?.RaiseCanExecuteChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    private string _stage1 = "(empty)";
    public string Stage1 { get => _stage1; private set => Set(ref _stage1, value); }

    private string _stage2 = "(empty)";
    public string Stage2 { get => _stage2; private set => Set(ref _stage2, value); }

    private string _upperChamber = "(empty)";
    public string UpperChamber { get => _upperChamber; private set => Set(ref _upperChamber, value); }

    private string _lowerChamber = "(empty)";
    public string LowerChamber { get => _lowerChamber; private set => Set(ref _lowerChamber, value); }

    private string _robot = "";
    public string Robot { get => _robot; private set => Set(ref _robot, value); }

    private string _axes = "";
    public string Axes { get => _axes; private set => Set(ref _axes, value); }

    // 설비 도형 바인딩용
    private bool _stage1HasMaterial;
    public bool Stage1HasMaterial { get => _stage1HasMaterial; private set => Set(ref _stage1HasMaterial, value); }

    private bool _stage2HasMaterial;
    public bool Stage2HasMaterial { get => _stage2HasMaterial; private set => Set(ref _stage2HasMaterial, value); }

    private bool _upperChamberHasMaterial;
    public bool UpperChamberHasMaterial { get => _upperChamberHasMaterial; private set => Set(ref _upperChamberHasMaterial, value); }

    private bool _lowerChamberHasMaterial;
    public bool LowerChamberHasMaterial { get => _lowerChamberHasMaterial; private set => Set(ref _lowerChamberHasMaterial, value); }

    private double _zPosition;
    public double ZPosition { get => _zPosition; private set => Set(ref _zPosition, value); }

    private RobotPosition _currentRobotPosition;
    public RobotPosition CurrentRobotPosition { get => _currentRobotPosition; private set => Set(ref _currentRobotPosition, value); }

    private void Cmd_LoadStage(object? commandParameter)
    {
        try
        {
            _sim.LoadStage(MaterialKind.UpperFilm, MaterialKind.LowerFilm);
            RefreshSnapshot();
        }
        catch (Exception ex)
        {
            Alarms.Insert(0, $"[VM] {ex.Message}");
        }
        finally { }
    }

    private void Cmd_StartAuto(object? commandParameter)
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

    private void Cmd_Stop(object? commandParameter)
    {
        try
        {
            _runCts?.Cancel();
        }
        catch (Exception ex)
        {
            Alarms.Insert(0, $"[VM] {ex.Message}");
        }
        finally { }
    }

    private void Cmd_Clear(object? commandParameter)
    {
        try
        {
            _sim.Reset();
            Alarms.Clear();
            RefreshSnapshot();
        }
        catch (Exception ex)
        {
            Alarms.Insert(0, $"[VM] {ex.Message}");
        }
        finally { }
    }

    private void Cmd_Exit(object? commandParameter)
    {
        App.Current.Shutdown();
    }

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

        ZPosition = _sim.LowerMotion.Z.Position;
        CurrentRobotPosition = _sim.Robot.CurrentPosition;

        Robot = $"{_sim.Robot.CurrentPosition} | Vacuum={_sim.Robot.HasVacuum}";
        Axes = $"U={_sim.LowerMotion.U.Position:0.0}, V={_sim.LowerMotion.V.Position:0.0}, W={_sim.LowerMotion.W.Position:0.0}, Z={_sim.LowerMotion.Z.Position:0.0}";
    }
}

