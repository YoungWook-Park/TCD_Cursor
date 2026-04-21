using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class Manual_MotorViewModel : NotifyPropertyChangedBase
{
    #region Fields

    private readonly MainCore _core = MainCore.Instance;
    private CancellationTokenSource? _activeCts;    // 현재 실행 중인 동작(Move/Home/Servo 등) 취소용
    private CancellationTokenSource? _jogCts;       // Jog 전용 취소용 (버튼 누름/뗌으로 제어)
    private readonly System.Windows.Threading.DispatcherTimer _statusTimer;

    private string _logStatus = "";
    private string _u = "0";
    private string _v = "0";
    private string _w = "0";
    private string _zLoad = "0";
    private string _zBond = "100";
    private string _selectedAxis = AxisDefine.U;
    private string _jogSpeed = "10";

    // 축별 AbsMove 시퀀스 키 테이블
    private static readonly IReadOnlyDictionary<string, string> AbsMoveSeqKeys = new Dictionary<string, string>
    {
        [AxisDefine.U]      = TcdSequenceKeys.Manual_Motor_U_AbsMove,
        [AxisDefine.V]      = TcdSequenceKeys.Manual_Motor_V_AbsMove,
        [AxisDefine.W]      = TcdSequenceKeys.Manual_Motor_W_AbsMove,
        [AxisDefine.ZLower] = TcdSequenceKeys.Manual_Motor_ZLower_AbsMove,
        [AxisDefine.ZUpper] = TcdSequenceKeys.Manual_Motor_ZUpper_AbsMove,
    };

    // 축별 Stop 시퀀스 키 테이블
    private static readonly IReadOnlyDictionary<string, string> StopSeqKeys = new Dictionary<string, string>
    {
        [AxisDefine.U]      = TcdSequenceKeys.Manual_Motor_U_Stop,
        [AxisDefine.V]      = TcdSequenceKeys.Manual_Motor_V_Stop,
        [AxisDefine.W]      = TcdSequenceKeys.Manual_Motor_W_Stop,
        [AxisDefine.ZLower] = TcdSequenceKeys.Manual_Motor_ZLower_Stop,
        [AxisDefine.ZUpper] = TcdSequenceKeys.Manual_Motor_ZUpper_Stop,
    };

    #endregion

    #region Constructor

    public Manual_MotorViewModel()
    {
        PullFromRecipe();

        foreach (var axisName in AxisDefine.InOrder)
            AxisStatuses.Add(new AxisStatusItem { AxisName = axisName });

        _statusTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _statusTimer.Tick += (_, _) => RefreshAxisStatus();
        _statusTimer.Start();
    }

    #endregion

    #region Properties

    /// <summary>마지막 동작 결과 메시지. XAML 상태 표시줄에 바인딩.</summary>
    public string LogStatus
    {
        get => _logStatus;
        private set => Set(ref _logStatus, value);
    }

    public string U          { get => _u;             set => Set(ref _u, value); }
    public string V          { get => _v;             set => Set(ref _v, value); }
    public string W          { get => _w;             set => Set(ref _w, value); }
    public string ZLoad      { get => _zLoad;         set => Set(ref _zLoad, value); }
    public string ZBond      { get => _zBond;         set => Set(ref _zBond, value); }
    public string SelectedAxis { get => _selectedAxis; set => Set(ref _selectedAxis, value); }
    public string JogSpeed   { get => _jogSpeed;      set => Set(ref _jogSpeed, value); }

    public ObservableCollection<string> Axes { get; } = new(AxisDefine.InOrder);
    public ObservableCollection<AxisStatusItem> AxisStatuses { get; } = new();

    #endregion

    #region Logic

    private void PullFromRecipe()
    {
        var recipe = _core.Recipes.Current;
        if (recipe == null) return;

        U     = recipe.GetAxis(AxisDefine.U).ToString("0.###");
        V     = recipe.GetAxis(AxisDefine.V).ToString("0.###");
        W     = recipe.GetAxis(AxisDefine.W).ToString("0.###");
        ZLoad = recipe.GetAxis(AxisDefine.ZLower).ToString("0.###");
        ZBond = recipe.GetAxis(AxisDefine.ZUpper).ToString("0.###");
    }

    private void TeachAxis(string axisKey, double position)
    {
        try
        {
            var recipe = _core.Recipes.Current ?? new TcdRecipe();
            recipe.SetAxis(axisKey, position);
            _core.RecipeRepository.Save(recipe);
            _core.Recipes.Current = recipe;
            PullFromRecipe();
            LogStatus = $"Taught {axisKey} = {position:0.###} (saved)";
        }
        catch (Exception ex)
        {
            LogStatus = ex.Message;
        }
    }

    private void MoveAxis(string axis)
    {
        if (!AbsMoveSeqKeys.TryGetValue(axis, out var seqKey)) return;
        RunOperation(seqKey, axis, "Move");
    }

    private void StopAxis(string axis)
    {
        // RunOperation 내부에서 _activeCts 취소 → 진행 중인 Move/Home/Servo 즉시 중단 후 Stop 실행
        if (!StopSeqKeys.TryGetValue(axis, out var seqKey)) return;
        RunOperation(seqKey, axis, "Stop");
    }

    private void StartJog(int direction)
    {
        if (!double.TryParse(JogSpeed, out var speed) || speed <= 0) return;

        _activeCts?.Cancel();   // Move 중이면 먼저 취소
        _jogCts?.Cancel();
        _jogCts = new CancellationTokenSource();

        var velocity = speed * direction;
        _ = Task.Run(async () =>
        {
            try
            {
                await _core.Motion.JogAsync(SelectedAxis, velocity, _jogCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogStatus = ex.Message; }
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
        _activeCts?.Cancel();
        _jogCts?.Cancel();
        foreach (var axis in Axes)
            _ = _core.Motion.StopAsync(axis, CancellationToken.None);
        LogStatus = "All motors stop requested.";
    }

    private double CurrentPosition(string axis) => _core.AxisStateProvider.GetAxisState(axis).Position;

    private void RefreshAxisStatus()
    {
        try
        {
            foreach (var item in AxisStatuses)
            {
                var axisState  = _core.AxisStateProvider.GetAxisState(item.AxisName);
                item.Position  = axisState.Position;
                item.IsMoving  = axisState.IsMoving;
                item.IsFault   = axisState.IsFault;
                item.IsHome    = axisState.IsHome;
                item.IsServoOn = axisState.IsServoOn;
                item.IsLimitPos = axisState.IsLimitPos;
                item.IsLimitNeg = axisState.IsLimitNeg;
            }
        }
        catch (Exception ex)
        {
            LogStatus = $"Status refresh error: {ex.Message}";
        }
    }

    /// <summary>
    /// 시퀀스를 비동기 실행. 이전 동작이 진행 중이면 취소 후 새 동작 시작.
    /// 같은 버튼을 다시 누르면 이전 시퀀스가 취소되고 재시작됨.
    /// </summary>
    private void RunOperation(string seqKey, string axisLabel, string actionLabel)
    {
        _activeCts?.Cancel();
        _activeCts?.Dispose();
        var cts = new CancellationTokenSource();
        _activeCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _core.Sequences.RunAsync(seqKey, _core.Simulation, null, cts.Token).ConfigureAwait(false);
                LogStatus = result.Status == SequenceStatus.Succeeded
                    ? $"{axisLabel} {actionLabel} completed"
                    : result.Error ?? $"{axisLabel} {actionLabel} failed";
            }
            catch (OperationCanceledException) { LogStatus = $"{axisLabel} {actionLabel} cancelled"; }
            catch (Exception ex)               { LogStatus = ex.Message; }
        });
    }

    #endregion

    #region Commands

    // ── Move ──────────────────────────────────────────────────────────────────
    private BiRelayCommand? cmd_MoveU;
    public ICommand Cmd_MoveU => cmd_MoveU ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.U));

    private BiRelayCommand? cmd_MoveV;
    public ICommand Cmd_MoveV => cmd_MoveV ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.V));

    private BiRelayCommand? cmd_MoveW;
    public ICommand Cmd_MoveW => cmd_MoveW ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.W));

    private BiRelayCommand? cmd_MoveZLoad;
    public ICommand Cmd_MoveZLoad => cmd_MoveZLoad ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.ZLower));

    private BiRelayCommand? cmd_MoveZBond;
    public ICommand Cmd_MoveZBond => cmd_MoveZBond ??= new BiRelayCommand(_ => MoveAxis(AxisDefine.ZUpper));

    // ── Teach ─────────────────────────────────────────────────────────────────
    private BiRelayCommand? cmd_TeachU;
    public ICommand Cmd_TeachU => cmd_TeachU ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.U, CurrentPosition(AxisDefine.U)));

    private BiRelayCommand? cmd_TeachV;
    public ICommand Cmd_TeachV => cmd_TeachV ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.V, CurrentPosition(AxisDefine.V)));

    private BiRelayCommand? cmd_TeachW;
    public ICommand Cmd_TeachW => cmd_TeachW ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.W, CurrentPosition(AxisDefine.W)));

    private BiRelayCommand? cmd_TeachZLoad;
    public ICommand Cmd_TeachZLoad => cmd_TeachZLoad ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.ZLower, CurrentPosition(AxisDefine.ZLower)));

    private BiRelayCommand? cmd_TeachZBond;
    public ICommand Cmd_TeachZBond => cmd_TeachZBond ??= new BiRelayCommand(_ => TeachAxis(AxisDefine.ZUpper, CurrentPosition(AxisDefine.ZUpper)));

    // ── Stop ──────────────────────────────────────────────────────────────────
    private BiRelayCommand? cmd_StopU;
    public ICommand Cmd_StopU => cmd_StopU ??= new BiRelayCommand(_ => StopAxis(AxisDefine.U));

    private BiRelayCommand? cmd_StopV;
    public ICommand Cmd_StopV => cmd_StopV ??= new BiRelayCommand(_ => StopAxis(AxisDefine.V));

    private BiRelayCommand? cmd_StopW;
    public ICommand Cmd_StopW => cmd_StopW ??= new BiRelayCommand(_ => StopAxis(AxisDefine.W));

    private BiRelayCommand? cmd_StopZLoad;
    public ICommand Cmd_StopZLoad => cmd_StopZLoad ??= new BiRelayCommand(_ => StopAxis(AxisDefine.ZLower));

    private BiRelayCommand? cmd_StopZBond;
    public ICommand Cmd_StopZBond => cmd_StopZBond ??= new BiRelayCommand(_ => StopAxis(AxisDefine.ZUpper));

    // ── Servo On / Servo Off / Home ───────────────────────────────────────────
    private BiRelayCommand? cmd_ServoOnU;
    public ICommand Cmd_ServoOnU => cmd_ServoOnU ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_U_ServoOn, "U", "Servo On"));

    private BiRelayCommand? cmd_ServoOffU;
    public ICommand Cmd_ServoOffU => cmd_ServoOffU ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_U_ServoOff, "U", "Servo Off"));

    private BiRelayCommand? cmd_HomeU;
    public ICommand Cmd_HomeU => cmd_HomeU ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_U_Home, "U", "Home"));

    private BiRelayCommand? cmd_ServoOnV;
    public ICommand Cmd_ServoOnV => cmd_ServoOnV ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_V_ServoOn, "V", "Servo On"));

    private BiRelayCommand? cmd_ServoOffV;
    public ICommand Cmd_ServoOffV => cmd_ServoOffV ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_V_ServoOff, "V", "Servo Off"));

    private BiRelayCommand? cmd_HomeV;
    public ICommand Cmd_HomeV => cmd_HomeV ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_V_Home, "V", "Home"));

    private BiRelayCommand? cmd_ServoOnW;
    public ICommand Cmd_ServoOnW => cmd_ServoOnW ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_W_ServoOn, "W", "Servo On"));

    private BiRelayCommand? cmd_ServoOffW;
    public ICommand Cmd_ServoOffW => cmd_ServoOffW ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_W_ServoOff, "W", "Servo Off"));

    private BiRelayCommand? cmd_HomeW;
    public ICommand Cmd_HomeW => cmd_HomeW ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_W_Home, "W", "Home"));

    private BiRelayCommand? cmd_ServoOnZLoad;
    public ICommand Cmd_ServoOnZLoad => cmd_ServoOnZLoad ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_ServoOn, "ZLoad", "Servo On"));

    private BiRelayCommand? cmd_ServoOffZLoad;
    public ICommand Cmd_ServoOffZLoad => cmd_ServoOffZLoad ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_ServoOff, "ZLoad", "Servo Off"));

    private BiRelayCommand? cmd_HomeZLoad;
    public ICommand Cmd_HomeZLoad => cmd_HomeZLoad ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_Home, "ZLoad", "Home"));

    private BiRelayCommand? cmd_ServoOnZBond;
    public ICommand Cmd_ServoOnZBond => cmd_ServoOnZBond ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOn, "ZBond", "Servo On"));

    private BiRelayCommand? cmd_ServoOffZBond;
    public ICommand Cmd_ServoOffZBond => cmd_ServoOffZBond ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOff, "ZBond", "Servo Off"));

    private BiRelayCommand? cmd_HomeZBond;
    public ICommand Cmd_HomeZBond => cmd_HomeZBond ??= new BiRelayCommand(_ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_Home, "ZBond", "Home"));

    // ── Jog ───────────────────────────────────────────────────────────────────
    private BiRelayCommand? cmd_JogPlusDown;
    public ICommand Cmd_JogPlusDown => cmd_JogPlusDown ??= new BiRelayCommand(_ => StartJog(+1));

    private BiRelayCommand? cmd_JogPlusUp;
    public ICommand Cmd_JogPlusUp => cmd_JogPlusUp ??= new BiRelayCommand(_ => StopJog());

    private BiRelayCommand? cmd_JogMinusDown;
    public ICommand Cmd_JogMinusDown => cmd_JogMinusDown ??= new BiRelayCommand(_ => StartJog(-1));

    private BiRelayCommand? cmd_JogMinusUp;
    public ICommand Cmd_JogMinusUp => cmd_JogMinusUp ??= new BiRelayCommand(_ => StopJog());

    // ── Stop All ──────────────────────────────────────────────────────────────
    private BiRelayCommand? cmd_StopAllMotors;
    public ICommand Cmd_StopAllMotors => cmd_StopAllMotors ??= new BiRelayCommand(_ => StopAllMotors());

    #endregion

    public sealed class AxisStatusItem : NotifyPropertyChangedBase
    {
        private string _axisName = "";
        private double _position;
        private bool _isServoOn;
        private bool _isHome;
        private bool _isMoving;
        private bool _isFault;
        private bool _isLimitPos;
        private bool _isLimitNeg;

        public string AxisName  { get => _axisName;   set => Set(ref _axisName, value); }
        public double Position  { get => _position;   set => Set(ref _position, value); }
        public bool IsServoOn   { get => _isServoOn;  set => Set(ref _isServoOn, value); }
        public bool IsHome      { get => _isHome;     set => Set(ref _isHome, value); }
        public bool IsMoving    { get => _isMoving;   set => Set(ref _isMoving, value); }
        public bool IsFault     { get => _isFault;    set => Set(ref _isFault, value); }
        public bool IsLimitPos  { get => _isLimitPos; set => Set(ref _isLimitPos, value); }
        public bool IsLimitNeg  { get => _isLimitNeg; set => Set(ref _isLimitNeg, value); }
    }
}
