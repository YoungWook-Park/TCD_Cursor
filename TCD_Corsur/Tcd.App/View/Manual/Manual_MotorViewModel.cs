using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;
using Tcd.App.Spii;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class Manual_MotorViewModel : NotifyPropertyChangedBase
{
    private readonly MainCore _core = MainCore.Instance;
    private CancellationTokenSource? _jogCts;
    private readonly System.Windows.Threading.DispatcherTimer _statusTimer;

    private string _status = "";
    public string Status { get => _status; private set => Set(ref _status, value); }

    private string _u = "0";
    public string U { get => _u; set => Set(ref _u, value); }
    private string _v = "0";
    public string V { get => _v; set => Set(ref _v, value); }
    private string _w = "0";
    public string W { get => _w; set => Set(ref _w, value); }
    private string _zLoad = "0";
    public string ZLoad { get => _zLoad; set => Set(ref _zLoad, value); }
    private string _zBond = "100";
    public string ZBond { get => _zBond; set => Set(ref _zBond, value); }

    // Jog settings
    public ObservableCollection<string> Axes { get; } = new() { "U", "V", "W", "ZUpper", "ZLower" };

    private string _selectedAxis = "U";
    public string SelectedAxis
    {
        get => _selectedAxis;
        set => Set(ref _selectedAxis, value);
    }

    private string _jogSpeed = "10";
    public string JogSpeed
    {
        get => _jogSpeed;
        set => Set(ref _jogSpeed, value);
    }

    // Motor status grid
    public sealed class AxisStatusItem : NotifyPropertyChangedBase
    {
        private string _axisName = "";
        public string AxisName
        {
            get => _axisName;
            set => Set(ref _axisName, value);
        }

        private double _position;
        public double Position
        {
            get => _position;
            set => Set(ref _position, value);
        }

        private bool _isHome;
        public bool IsHome
        {
            get => _isHome;
            set => Set(ref _isHome, value);
        }

        private bool _isMoving;
        public bool IsMoving
        {
            get => _isMoving;
            set => Set(ref _isMoving, value);
        }

        private bool _isFault;
        public bool IsFault
        {
            get => _isFault;
            set => Set(ref _isFault, value);
        }
    }

    public ObservableCollection<AxisStatusItem> AxisStatuses { get; } = new();

    public Manual_MotorViewModel()
    {
        PullFromRecipe();

        // initialize axis status rows
        AxisStatuses.Add(new AxisStatusItem { AxisName = "U" });
        AxisStatuses.Add(new AxisStatusItem { AxisName = "V" });
        AxisStatuses.Add(new AxisStatusItem { AxisName = "W" });
        AxisStatuses.Add(new AxisStatusItem { AxisName = "ZUpper" });
        AxisStatuses.Add(new AxisStatusItem { AxisName = "ZLower" });

        _statusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _statusTimer.Tick += (_, _) => RefreshAxisStatus();
        _statusTimer.Start();
    }

    // Axis move / teach
    public ICommand Cmd_MoveU => new BiRelayCommand(_ => MoveAxis("U", Parse(U)));
    public ICommand Cmd_MoveV => new BiRelayCommand(_ => MoveAxis("V", Parse(V)));
    public ICommand Cmd_MoveW => new BiRelayCommand(_ => MoveAxis("W", Parse(W)));
    public ICommand Cmd_MoveZLoad => new BiRelayCommand(_ => MoveAxis("Z", Parse(ZLoad)));
    public ICommand Cmd_MoveZBond => new BiRelayCommand(_ => MoveAxis("Z", Parse(ZBond)));

    public ICommand Cmd_TeachU => new BiRelayCommand(_ => TeachAxis("U", CurrentU()));
    public ICommand Cmd_TeachV => new BiRelayCommand(_ => TeachAxis("V", CurrentV()));
    public ICommand Cmd_TeachW => new BiRelayCommand(_ => TeachAxis("W", CurrentW()));
    public ICommand Cmd_TeachZLoad => new BiRelayCommand(_ => TeachAxis("Z_Load", CurrentZ()));
    public ICommand Cmd_TeachZBond => new BiRelayCommand(_ => TeachAxis("Z_Bond", CurrentZ()));

    // Jog commands (for Behavior)
    private BiRelayCommand? cmd_JogPlusDown;
    public ICommand Cmd_JogPlusDown => cmd_JogPlusDown ??= new BiRelayCommand(_ => StartJog(+1));

    private BiRelayCommand? cmd_JogPlusUp;
    public ICommand Cmd_JogPlusUp => cmd_JogPlusUp ??= new BiRelayCommand(_ => StopJog());

    private BiRelayCommand? cmd_JogMinusDown;
    public ICommand Cmd_JogMinusDown => cmd_JogMinusDown ??= new BiRelayCommand(_ => StartJog(-1));

    private BiRelayCommand? cmd_JogMinusUp;
    public ICommand Cmd_JogMinusUp => cmd_JogMinusUp ??= new BiRelayCommand(_ => StopJog());

    // Stop all motors button
    private BiRelayCommand? cmd_StopAllMotors;
    public ICommand Cmd_StopAllMotors => cmd_StopAllMotors ??= new BiRelayCommand(_ => StopAllMotors());

    private void PullFromRecipe()
    {
        var r = _core.Recipes.Current;
        if (r == null) return;
        U = r.GetAxis("U").ToString("0.###");
        V = r.GetAxis("V").ToString("0.###");
        W = r.GetAxis("W").ToString("0.###");
        ZLoad = r.GetAxis("Z_Load").ToString("0.###");
        ZBond = r.GetAxis("Z_Bond").ToString("0.###");
    }

    private void TeachAxis(string key, double value)
    {
        try
        {
            var r = _core.Recipes.Current ?? new TcdRecipe();
            r.SetAxis(key, value);
            _core.RecipeRepository.Save(r);
            _core.Recipes.Current = r;
            PullFromRecipe();
            Status = $"Taught {key} = {value:0.###} (saved)";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private void MoveAxis(string axis, double target)
    {
        _ = RunAsync(async ct =>
        {
            if (axis == "U")
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Axis0_AbsMove, _core.Simulation, target, ct).ConfigureAwait(false);
                Status = result.Status == Tcd.Sequence.SequenceStatus.Succeeded ? $"U at {target:0.###}" : result.Error ?? "U move failed";
            }
            else
            {
                await _core.Motion.AbsMoveAsync(axis, target, ct).ConfigureAwait(false);
                Status = $"{axis} at {target:0.###}";
            }
        });
    }

    private void StartJog(int direction)
    {
        if (!double.TryParse(JogSpeed, out var spd) || spd <= 0) return;
        var vel = spd * direction;

        _jogCts?.Cancel();
        _jogCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await _core.Motion.JogAsync(SelectedAxis, vel, _jogCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        });
    }

    private void StopJog()
    {
        _jogCts?.Cancel();
        _jogCts = null;
        _ = _core.Motion.StopAsync(SelectedAxis, CancellationToken.None);
    }

    private void StopAllMotors()
    {
        foreach (var axis in Axes)
        {
            _ = _core.Motion.StopAsync(axis, CancellationToken.None);
        }
        Status = "All motors stop requested.";
    }

    private double CurrentU() => _core.Simulation.LowerMotion.U.Position;
    private double CurrentV() => _core.Simulation.LowerMotion.V.Position;
    private double CurrentW() => _core.Simulation.LowerMotion.W.Position;
    private double CurrentZ() => _core.Simulation.LowerMotion.Z.Position;

    private static double Parse(string s) => double.TryParse(s, out var v) ? v : 0;

    private void RefreshAxisStatus()
    {
        try
        {
            // SPIIPlus 사용 시: ACS 모니터링 버퍼에서 실제 위치를 읽어 온다.
            if (_core.Settings.UseSpiiPlus && _core.Motion is SpiiPlusMotionService spii)
            {
                foreach (var item in AxisStatuses)
                {
                    item.Position = spii.GetActualPosition(item.AxisName);
                }
            }
            else
            {
                // 시뮬레이터 모드에서는 기존 시뮬레이션 객체에서 위치를 읽는다.
                foreach (var item in AxisStatuses)
                {
                    switch (item.AxisName)
                    {
                        case "U":
                            item.Position = _core.Simulation.LowerMotion.U.Position;
                            break;
                        case "V":
                            item.Position = _core.Simulation.LowerMotion.V.Position;
                            break;
                        case "W":
                            item.Position = _core.Simulation.LowerMotion.W.Position;
                            break;
                        case "ZUpper":
                            item.Position = _core.Simulation.LowerMotion.Z.Position;
                            break;
                        case "ZLower":
                            item.Position = _core.Simulation.LowerMotion.Z.Position;
                            break;
                    }
                }
            }
        }
        catch
        {
            // ignore UI refresh errors
        }
    }

    private Task RunAsync(Func<CancellationToken, Task> body)
    {
        return Task.Run(async () =>
        {
            try
            {
                await body(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        });
    }

    #region Ui Function

    private BiRelayCommand cmd_StopU;
    public ICommand Cmd_StopU => cmd_StopU ??= new BiRelayCommand(PerformCmd_StopU);

    private void PerformCmd_StopU(object commandParameter)
    {
        _ = RunAsync(async ct =>
        {
            var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Axis0_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
            Status = result.Status == Tcd.Sequence.SequenceStatus.Succeeded ? "U stop completed" : result.Error ?? "U stop failed";
        });
    }

    //-----------------------------------------------------------
    private BiRelayCommand cmd_StopV;
    public ICommand Cmd_StopV => cmd_StopV ??= new BiRelayCommand(PerformCmd_StopV);

    private void PerformCmd_StopV(object commandParameter)
    {
        try
        {
            _core.Motion.StopAsync("V", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Status = ex.Message;

        }
        finally
        {

        }
    }

    //-----------------------------------------------------------
    private BiRelayCommand cmd_StopZBond;
    public ICommand Cmd_StopZBond => cmd_StopZBond ??= new BiRelayCommand(PerformCmd_StopZBond);

    private void PerformCmd_StopZBond(object commandParameter)
    {
        try
        {
            _core.Motion.StopAsync("ZUpper", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Status = ex.Message;

        }
        finally
        {

        }
    }

    //-----------------------------------------------------------
    private BiRelayCommand cmd_StopZLoad;
    public ICommand Cmd_StopZLoad => cmd_StopZLoad ??= new BiRelayCommand(PerformCmd_StopZLoad);

    private void PerformCmd_StopZLoad(object commandParameter)
    {
        try
        {
            _core.Motion.StopAsync("ZLower", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Status = ex.Message;

        }
        finally
        {

        }
    }

    //-----------------------------------------------------------
    private BiRelayCommand cmd_StopW;
    public ICommand Cmd_StopW => cmd_StopW ??= new BiRelayCommand(PerformCmd_StopW);

    private void PerformCmd_StopW(object commandParameter)
    {
        try
        {
            _core.Motion.StopAsync("W", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Status = ex.Message;

        }
        finally
        {

        }
    }
    #endregion
}

