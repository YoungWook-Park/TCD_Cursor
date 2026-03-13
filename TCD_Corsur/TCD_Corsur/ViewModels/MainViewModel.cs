using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using TCD_Corsur.Mvvm;
using Tcd.Core;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;

namespace TCD_Corsur.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly TcdSimulation _sim = new TcdSimulation();
        private readonly DispatcherTimer _uiTimer;
        private CancellationTokenSource _runCts;
        private bool _isRunning;
        private string _status;

        public MainViewModel()
        {
            Status = "Idle";

            StartCommand = new RelayCommand(Start, () => !IsRunning);
            StopCommand = new RelayCommand(Stop, () => IsRunning);
            LoadStageCommand = new RelayCommand(() =>
            {
                _sim.LoadStage(MaterialKind.UpperFilm, MaterialKind.LowerFilm);
                RefreshSnapshot();
            }, () => !IsRunning);
            ClearCommand = new RelayCommand(() =>
            {
                _sim.Reset();
                Alarms.Clear();
                RefreshSnapshot();
            }, () => !IsRunning);

            var am = _sim.Alarms as AlarmManager;
            if (am != null)
            {
                am.AlarmRaised += (s, a) => App.Current.Dispatcher.Invoke(() => Alarms.Insert(0, a.ToString()));
            }

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _uiTimer.Tick += (s, e) => RefreshSnapshot();
            _uiTimer.Start();
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand LoadStageCommand { get; }
        public RelayCommand ClearCommand { get; }

        public ObservableCollection<string> Alarms { get; } = new ObservableCollection<string>();

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (Set(ref _isRunning, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                    LoadStageCommand.RaiseCanExecuteChanged();
                    ClearCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            private set => Set(ref _status, value);
        }

        private string _stage1;
        public string Stage1 { get => _stage1; private set => Set(ref _stage1, value); }

        private string _stage2;
        public string Stage2 { get => _stage2; private set => Set(ref _stage2, value); }

        private string _upperChamber;
        public string UpperChamber { get => _upperChamber; private set => Set(ref _upperChamber, value); }

        private string _lowerChamber;
        public string LowerChamber { get => _lowerChamber; private set => Set(ref _lowerChamber, value); }

        private string _robot;
        public string Robot { get => _robot; private set => Set(ref _robot, value); }

        private string _axes;
        public string Axes { get => _axes; private set => Set(ref _axes, value); }

        private void Start()
        {
            IsRunning = true;
            Status = "Running...";

            _runCts = new CancellationTokenSource();
            _sim.BindStopToken(_runCts.Token);

            var graph = TcdAutoSequenceFactory.Create(_sim, stageLoadTimeout: TimeSpan.FromSeconds(5));
            var runner = new SequenceRunner(graph);

            Task.Run(async () =>
            {
                try
                {
                    var result = await runner.RunAsync(_sim, _runCts.Token).ConfigureAwait(false);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Status = result.Status == NodeRunStatus.Succeeded ? "Done" : $"Stopped/Failed: {result.Status}";
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

        private void Stop()
        {
            _runCts?.Cancel();
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

            Robot = $"{_sim.Robot.CurrentPosition} | Vacuum={_sim.Robot.HasVacuum}";
            Axes = $"U={_sim.LowerMotion.U.Position:0.0}, V={_sim.LowerMotion.V.Position:0.0}, W={_sim.LowerMotion.W.Position:0.0}, Z={_sim.LowerMotion.Z.Position:0.0}";
        }
    }
}

