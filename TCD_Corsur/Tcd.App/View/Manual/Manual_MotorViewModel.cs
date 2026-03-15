using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class Manual_MotorViewModel : NotifyPropertyChangedBase
{
    #region Variable
    private readonly MainCore _core = MainCore.Instance;
    private CancellationTokenSource? _jogCts;
    private readonly System.Windows.Threading.DispatcherTimer _statusTimer;

    private string _status = "";
    private string _u = "0";
    private string _v = "0";
    private string _w = "0";
    private string _zLoad = "0";
    private string _zBond = "100";
    private string _selectedAxis = AxisDefine.U;
    private string _jogSpeed = "10";

    #endregion
    #region Constructor

    public Manual_MotorViewModel()
    {
        PullFromRecipe();

        foreach (var name in AxisDefine.InOrder)
        {
            AxisStatuses.Add(new AxisStatusItem { AxisName = name });
        }

        _statusTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _statusTimer.Tick += (_, _) => RefreshAxisStatus();
        _statusTimer.Start();
    }

    #endregion
    #region Property

    public string Status
    {
        get { return _status; }
        private set { Set(ref _status, value); }
    }

    public string U
    {
        get { return _u; }
        set { Set(ref _u, value); }
    }

    public string V
    {
        get { return _v; }
        set { Set(ref _v, value); }
    }

    public string W
    {
        get { return _w; }
        set { Set(ref _w, value); }
    }

    public string ZLoad
    {
        get { return _zLoad; }
        set { Set(ref _zLoad, value); }
    }

    public string ZBond
    {
        get { return _zBond; }
        set { Set(ref _zBond, value); }
    }

    public string SelectedAxis
    {
        get { return _selectedAxis; }
        set { Set(ref _selectedAxis, value); }
    }

    public string JogSpeed
    {
        get { return _jogSpeed; }
        set { Set(ref _jogSpeed, value); }
    }

    public ObservableCollection<string> Axes { get; } = new(AxisDefine.InOrder);
    public ObservableCollection<AxisStatusItem> AxisStatuses { get; } = new();

    #endregion
    #region Function

    private void PullFromRecipe()
    {
        var r = _core.Recipes.Current;
        if (r == null)
        {
            return;
        }

        U = r.GetAxis(AxisDefine.U).ToString("0.###");
        V = r.GetAxis(AxisDefine.V).ToString("0.###");
        W = r.GetAxis(AxisDefine.W).ToString("0.###");
        ZLoad = r.GetAxis(AxisDefine.ZLower).ToString("0.###");
        ZBond = r.GetAxis(AxisDefine.ZUpper).ToString("0.###");
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
        finally
        {
        }
    }

    private string GetAbsMoveKey(string axis)
    {
        switch (axis)
        {
            case var _ when axis == AxisDefine.U:
                return TcdSequenceKeys.Manual_Motor_U_AbsMove;
            case var _ when axis == AxisDefine.V:
                return TcdSequenceKeys.Manual_Motor_V_AbsMove;
            case var _ when axis == AxisDefine.W:
                return TcdSequenceKeys.Manual_Motor_W_AbsMove;
            case var _ when axis == AxisDefine.ZLower:
                return TcdSequenceKeys.Manual_Motor_ZLower_AbsMove;
            case var _ when axis == AxisDefine.ZUpper:
                return TcdSequenceKeys.Manual_Motor_ZUpper_AbsMove;
            default:
                return string.Empty;
        }
    }

    private void MoveAxis(string axis, double target)
    {
        var key = GetAbsMoveKey(axis);
        _ = RunAsync(async ct =>
        {
            if (!string.IsNullOrEmpty(key))
            {
                var result = await _core.Sequences.RunAsync(key, _core.Simulation, null, ct).ConfigureAwait(false);
                var recipeTarget = _core.Recipes.Current?.GetAxis(axis, 0) ?? target;
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = $"{axis} at {recipeTarget:0.###}";
                }
                else
                {
                    Status = result.Error ?? $"{axis} move failed";
                }
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
        if (!double.TryParse(JogSpeed, out var spd) || spd <= 0)
        {
            return;
        }

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

    private double CurrentPosition(string axis) => _core.AxisStateProvider.GetAxisState(axis).Position;
    private double CurrentU() => CurrentPosition(AxisDefine.U);
    private double CurrentV() => CurrentPosition(AxisDefine.V);
    private double CurrentW() => CurrentPosition(AxisDefine.W);

    private static double Parse(string s)
    {
        if (double.TryParse(s, out var v))
        {
            return v;
        }
        return 0;
    }

    private void RefreshAxisStatus()
    {
        try
        {
            foreach (var item in AxisStatuses)
            {
                var s = _core.AxisStateProvider.GetAxisState(item.AxisName);
                item.Position = s.Position;
                item.IsMoving = s.IsMoving;
                item.IsFault = s.IsFault;
                item.IsHome = s.IsHome;
            }
        }
        catch
        {
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

    #endregion
    #region UI Function

    private BiRelayCommand? cmd_MoveU;
    public ICommand Cmd_MoveU
    {
        get
        {
            return cmd_MoveU ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.U, Parse(U)));
        }
    }

    private BiRelayCommand? cmd_MoveV;
    public ICommand Cmd_MoveV
    {
        get
        {
            return cmd_MoveV ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.V, Parse(V)));
        }
    }

    private BiRelayCommand? cmd_MoveW;
    public ICommand Cmd_MoveW
    {
        get
        {
            return cmd_MoveW ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.W, Parse(W)));
        }
    }

    private BiRelayCommand? cmd_MoveZLoad;
    public ICommand Cmd_MoveZLoad
    {
        get
        {
            return cmd_MoveZLoad ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.ZLower, Parse(ZLoad)));
        }
    }

    private BiRelayCommand? cmd_MoveZBond;
    public ICommand Cmd_MoveZBond
    {
        get
        {
            return cmd_MoveZBond ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.ZUpper, Parse(ZBond)));
        }
    }

    private BiRelayCommand? cmd_TeachU;
    public ICommand Cmd_TeachU
    {
        get
        {
            return cmd_TeachU ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.U, CurrentU()));
        }
    }

    private BiRelayCommand? cmd_TeachV;
    public ICommand Cmd_TeachV
    {
        get
        {
            return cmd_TeachV ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.V, CurrentV()));
        }
    }

    private BiRelayCommand? cmd_TeachW;
    public ICommand Cmd_TeachW
    {
        get
        {
            return cmd_TeachW ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.W, CurrentW()));
        }
    }

    private BiRelayCommand? cmd_TeachZLoad;
    public ICommand Cmd_TeachZLoad
    {
        get
        {
            return cmd_TeachZLoad ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.ZLower, CurrentPosition(AxisDefine.ZLower)));
        }
    }

    private BiRelayCommand? cmd_TeachZBond;
    public ICommand Cmd_TeachZBond
    {
        get
        {
            return cmd_TeachZBond ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.ZUpper, CurrentPosition(AxisDefine.ZUpper)));
        }
    }

    private BiRelayCommand? cmd_JogPlusDown;
    public ICommand Cmd_JogPlusDown
    {
        get
        {
            return cmd_JogPlusDown ??= new BiRelayCommand(_ => StartJog(+1));
        }
    }

    private BiRelayCommand? cmd_JogPlusUp;
    public ICommand Cmd_JogPlusUp
    {
        get
        {
            return cmd_JogPlusUp ??= new BiRelayCommand(_ => StopJog());
        }
    }

    private BiRelayCommand? cmd_JogMinusDown;
    public ICommand Cmd_JogMinusDown
    {
        get
        {
            return cmd_JogMinusDown ??= new BiRelayCommand(_ => StartJog(-1));
        }
    }

    private BiRelayCommand? cmd_JogMinusUp;
    public ICommand Cmd_JogMinusUp
    {
        get
        {
            return cmd_JogMinusUp ??= new BiRelayCommand(_ => StopJog());
        }
    }

    private BiRelayCommand? cmd_StopAllMotors;
    public ICommand Cmd_StopAllMotors
    {
        get
        {
            return cmd_StopAllMotors ??= new BiRelayCommand(_ => StopAllMotors());
        }
    }

    private BiRelayCommand? cmd_StopU;
    public ICommand Cmd_StopU
    {
        get
        {
            return cmd_StopU ??= new BiRelayCommand(PerformCmd_StopU);
        }
    }

    private void PerformCmd_StopU(object? commandParameter)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Motor_U_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = "U stop completed";
                }
                else
                {
                    Status = result.Error ?? "U stop failed";
                }
            });
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
        }
    }

    private BiRelayCommand? cmd_StopV;
    public ICommand Cmd_StopV
    {
        get
        {
            return cmd_StopV ??= new BiRelayCommand(PerformCmd_StopV);
        }
    }

    private void PerformCmd_StopV(object? commandParameter)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Motor_V_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = "V stop completed";
                }
                else
                {
                    Status = result.Error ?? "V stop failed";
                }
            });
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
        }
    }

    private BiRelayCommand? cmd_StopZBond;
    public ICommand Cmd_StopZBond
    {
        get
        {
            return cmd_StopZBond ??= new BiRelayCommand(PerformCmd_StopZBond);
        }
    }

    private void PerformCmd_StopZBond(object? commandParameter)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Motor_ZUpper_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = "ZUpper stop completed";
                }
                else
                {
                    Status = result.Error ?? "ZUpper stop failed";
                }
            });
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
        }
    }

    private BiRelayCommand? cmd_StopZLoad;
    public ICommand Cmd_StopZLoad
    {
        get
        {
            return cmd_StopZLoad ??= new BiRelayCommand(PerformCmd_StopZLoad);
        }
    }

    private void PerformCmd_StopZLoad(object? commandParameter)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Motor_ZLower_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = "ZLower stop completed";
                }
                else
                {
                    Status = result.Error ?? "ZLower stop failed";
                }
            });
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
        }
    }

    private BiRelayCommand? cmd_StopW;
    public ICommand Cmd_StopW
    {
        get
        {
            return cmd_StopW ??= new BiRelayCommand(PerformCmd_StopW);
        }
    }

    private void PerformCmd_StopW(object? commandParameter)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(TcdSequenceKeys.Manual_Motor_W_Stop, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = "W stop completed";
                }
                else
                {
                    Status = result.Error ?? "W stop failed";
                }
            });
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
        }
    }

    // -------- U: Servo On / Servo Off / Home --------
    private BiRelayCommand? cmd_ServoOnU;
    public ICommand Cmd_ServoOnU { get { return cmd_ServoOnU ??= new BiRelayCommand(PerformCmd_ServoOnU); } }
    private void PerformCmd_ServoOnU(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_U_ServoOn, "U", "Servo On"); }

    private BiRelayCommand? cmd_ServoOffU;
    public ICommand Cmd_ServoOffU { get { return cmd_ServoOffU ??= new BiRelayCommand(PerformCmd_ServoOffU); } }
    private void PerformCmd_ServoOffU(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_U_ServoOff, "U", "Servo Off"); }

    private BiRelayCommand? cmd_HomeU;
    public ICommand Cmd_HomeU { get { return cmd_HomeU ??= new BiRelayCommand(PerformCmd_HomeU); } }
    private void PerformCmd_HomeU(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_U_Home, "U", "Home"); }

    // -------- V --------
    private BiRelayCommand? cmd_ServoOnV;
    public ICommand Cmd_ServoOnV { get { return cmd_ServoOnV ??= new BiRelayCommand(PerformCmd_ServoOnV); } }
    private void PerformCmd_ServoOnV(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_V_ServoOn, "V", "Servo On"); }

    private BiRelayCommand? cmd_ServoOffV;
    public ICommand Cmd_ServoOffV { get { return cmd_ServoOffV ??= new BiRelayCommand(PerformCmd_ServoOffV); } }
    private void PerformCmd_ServoOffV(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_V_ServoOff, "V", "Servo Off"); }

    private BiRelayCommand? cmd_HomeV;
    public ICommand Cmd_HomeV { get { return cmd_HomeV ??= new BiRelayCommand(PerformCmd_HomeV); } }
    private void PerformCmd_HomeV(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_V_Home, "V", "Home"); }

    // -------- W --------
    private BiRelayCommand? cmd_ServoOnW;
    public ICommand Cmd_ServoOnW { get { return cmd_ServoOnW ??= new BiRelayCommand(PerformCmd_ServoOnW); } }
    private void PerformCmd_ServoOnW(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_W_ServoOn, "W", "Servo On"); }

    private BiRelayCommand? cmd_ServoOffW;
    public ICommand Cmd_ServoOffW { get { return cmd_ServoOffW ??= new BiRelayCommand(PerformCmd_ServoOffW); } }
    private void PerformCmd_ServoOffW(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_W_ServoOff, "W", "Servo Off"); }

    private BiRelayCommand? cmd_HomeW;
    public ICommand Cmd_HomeW { get { return cmd_HomeW ??= new BiRelayCommand(PerformCmd_HomeW); } }
    private void PerformCmd_HomeW(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_W_Home, "W", "Home"); }

    // -------- Z Load --------
    private BiRelayCommand? cmd_ServoOnZLoad;
    public ICommand Cmd_ServoOnZLoad { get { return cmd_ServoOnZLoad ??= new BiRelayCommand(PerformCmd_ServoOnZLoad); } }
    private void PerformCmd_ServoOnZLoad(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZLower_ServoOn, "ZLoad", "Servo On"); }

    private BiRelayCommand? cmd_ServoOffZLoad;
    public ICommand Cmd_ServoOffZLoad { get { return cmd_ServoOffZLoad ??= new BiRelayCommand(PerformCmd_ServoOffZLoad); } }
    private void PerformCmd_ServoOffZLoad(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZLower_ServoOff, "ZLoad", "Servo Off"); }

    private BiRelayCommand? cmd_HomeZLoad;
    public ICommand Cmd_HomeZLoad { get { return cmd_HomeZLoad ??= new BiRelayCommand(PerformCmd_HomeZLoad); } }
    private void PerformCmd_HomeZLoad(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZLower_Home, "ZLoad", "Home"); }

    // -------- Z Bond --------
    private BiRelayCommand? cmd_ServoOnZBond;
    public ICommand Cmd_ServoOnZBond { get { return cmd_ServoOnZBond ??= new BiRelayCommand(PerformCmd_ServoOnZBond); } }
    private void PerformCmd_ServoOnZBond(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOn, "ZBond", "Servo On"); }

    private BiRelayCommand? cmd_ServoOffZBond;
    public ICommand Cmd_ServoOffZBond { get { return cmd_ServoOffZBond ??= new BiRelayCommand(PerformCmd_ServoOffZBond); } }
    private void PerformCmd_ServoOffZBond(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOff, "ZBond", "Servo Off"); }

    private BiRelayCommand? cmd_HomeZBond;
    public ICommand Cmd_HomeZBond { get { return cmd_HomeZBond ??= new BiRelayCommand(PerformCmd_HomeZBond); } }
    private void PerformCmd_HomeZBond(object? _) { RunAxisSequence(TcdSequenceKeys.Manual_Motor_ZUpper_Home, "ZBond", "Home"); }

    private void RunAxisSequence(string key, string axisLabel, string actionLabel)
    {
        try
        {
            _ = RunAsync(async ct =>
            {
                var result = await _core.Sequences.RunAsync(key, _core.Simulation, null, ct).ConfigureAwait(false);
                if (result.Status == Tcd.Sequence.SequenceStatus.Succeeded)
                {
                    Status = $"{axisLabel} {actionLabel} completed";
                }
                else
                {
                    Status = result.Error ?? $"{axisLabel} {actionLabel} failed";
                }
            });
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

    public sealed class AxisStatusItem : NotifyPropertyChangedBase
    {
        private string _axisName = "";
        private double _position;
        private bool _isHome;
        private bool _isMoving;
        private bool _isFault;

        public string AxisName
        {
            get { return _axisName; }
            set { Set(ref _axisName, value); }
        }

        public double Position
        {
            get { return _position; }
            set { Set(ref _position, value); }
        }

        public bool IsHome
        {
            get { return _isHome; }
            set { Set(ref _isHome, value); }
        }

        public bool IsMoving
        {
            get { return _isMoving; }
            set { Set(ref _isMoving, value); }
        }

        public bool IsFault
        {
            get { return _isFault; }
            set { Set(ref _isFault, value); }
        }
    }
}

