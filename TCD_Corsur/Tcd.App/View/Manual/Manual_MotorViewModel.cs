using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Mvvm;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class Manual_MotorViewModel : NotifyPropertyChangedBase
{
  #region Fields

  private readonly MainCore _core = MainCore.Instance;
  private CancellationTokenSource? _activeCts;
  private CancellationTokenSource? _jogCts;
  private readonly System.Windows.Threading.DispatcherTimer _statusTimer;

  private string _logStatus = "";
  private string _u      = "0";
  private string _v      = "0";
  private string _w      = "0";
  private string _zLoad  = "0";
  private string _zBond  = "100";
  private string _selectedAxis = AxisDefine.U;
  private string _jogSpeed = "10";

  private static readonly IReadOnlyDictionary<string, string> AbsMoveSeqKeys =
    new Dictionary<string, string>
    {
      [AxisDefine.U]      = TcdSequenceKeys.Manual_Motor_U_AbsMove,
      [AxisDefine.V]      = TcdSequenceKeys.Manual_Motor_V_AbsMove,
      [AxisDefine.W]      = TcdSequenceKeys.Manual_Motor_W_AbsMove,
      [AxisDefine.ZLower] = TcdSequenceKeys.Manual_Motor_ZLower_AbsMove,
      [AxisDefine.ZUpper] = TcdSequenceKeys.Manual_Motor_ZUpper_AbsMove,
    };

  private static readonly IReadOnlyDictionary<string, Func<Manual_MotorViewModel, string>>
    AxisTextBoxGetters = new Dictionary<string, Func<Manual_MotorViewModel, string>>
    {
      [AxisDefine.U]      = vm => vm.U,
      [AxisDefine.V]      = vm => vm.V,
      [AxisDefine.W]      = vm => vm.W,
      [AxisDefine.ZLower] = vm => vm.ZLoad,
      [AxisDefine.ZUpper] = vm => vm.ZBond,
    };

  private static readonly IReadOnlyDictionary<string, string> StopSeqKeys =
    new Dictionary<string, string>
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
    foreach (var axisName in AxisDefine.InOrder)
      AxisStatuses.Add(new AxisStatusItem { AxisName = axisName });

    InitTeachGroups();

    _statusTimer = new System.Windows.Threading.DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(200)
    };
    _statusTimer.Tick += (_, _) => RefreshAxisStatus();
    _statusTimer.Start();
  }

  #endregion

  #region Properties

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  public string U      { get => _u;     set => Set(ref _u, value); }
  public string V      { get => _v;     set => Set(ref _v, value); }
  public string W      { get => _w;     set => Set(ref _w, value); }
  public string ZLoad  { get => _zLoad; set => Set(ref _zLoad, value); }
  public string ZBond  { get => _zBond; set => Set(ref _zBond, value); }

  public string SelectedAxis
  {
    get => _selectedAxis;
    set => Set(ref _selectedAxis, value);
  }

  public string JogSpeed
  {
    get => _jogSpeed;
    set => Set(ref _jogSpeed, value);
  }

  public ObservableCollection<string> Axes { get; } = new(AxisDefine.InOrder);
  public ObservableCollection<AxisStatusItem> AxisStatuses { get; } = new();

  /// <summary>Teach 탭: 축별 고정 티칭 그룹.</summary>
  public ObservableCollection<AxisTeachGroup> AxisTeachGroups { get; } = new();

  #endregion

  #region Teach Groups

  private void InitTeachGroups()
  {
    AxisTeachGroups.Clear();

    AxisTeachGroups.Add(BuildGroup(AxisDefine.ZLower, "ZLower (StageZ)",
      MotorTeachKeys.ZLowerKeys));

    foreach (var axis in new[] { AxisDefine.U, AxisDefine.V, AxisDefine.W })
      AxisTeachGroups.Add(BuildGroup(axis, axis, MotorTeachKeys.UvwKeys));

    AxisTeachGroups.Add(BuildGroup(AxisDefine.ZUpper, "ZUpper",
      MotorTeachKeys.ZUpperKeys));

    LoadTeachValues();
  }

  private AxisTeachGroup BuildGroup(
    string axisName, string displayName, IEnumerable<string> keys)
  {
    var group = new AxisTeachGroup { AxisName = axisName, DisplayName = displayName };
    foreach (var k in keys)
      group.Points.Add(new AxisTeachPoint { Name = k, Position = 0 });
    return group;
  }

  private void LoadTeachValues()
  {
    var recipe = _core.Recipes.Current;
    if (recipe == null) return;

    foreach (var group in AxisTeachGroups)
      foreach (var pt in group.Points)
        pt.Position = recipe.GetNamedPosition(group.AxisName, pt.Name);
  }

  private void SaveTeachValues()
  {
    var recipe = _core.Recipes.Current ?? new TcdRecipe();
    foreach (var group in AxisTeachGroups)
      foreach (var pt in group.Points)
        recipe.SetNamedPosition(group.AxisName, pt.Name, pt.Position);
    _core.RecipeRepository.Save(recipe);
    _core.Recipes.Current = recipe;
    LogStatus = "Teach positions saved.";
  }

  #endregion

  #region Logic

  private void MoveToTeachPoint(AxisTeachPoint? pt, string axisName)
  {
    if (pt == null) return;
    if (!AbsMoveSeqKeys.TryGetValue(axisName, out var seqKey)) return;
    RunOperation(seqKey, axisName, $"→ {pt.Name}", pt.Position);
  }

  private void TeachCurrentPosition(AxisTeachPoint? pt, string axisName)
  {
    if (pt == null) return;
    pt.Position = CurrentPosition(axisName);
    var recipe = _core.Recipes.Current ?? new TcdRecipe();
    recipe.SetNamedPosition(axisName, pt.Name, pt.Position);
    _core.RecipeRepository.Save(recipe);
    _core.Recipes.Current = recipe;
    LogStatus = $"Taught {axisName}.{pt.Name} = {pt.Position:0.###}";
  }

  private void MoveAxis(string axis)
  {
    if (!AbsMoveSeqKeys.TryGetValue(axis, out var seqKey)) return;
    if (!AxisTextBoxGetters.TryGetValue(axis, out var getter)) return;
    if (!double.TryParse(getter(this),
          System.Globalization.NumberStyles.Float,
          System.Globalization.CultureInfo.InvariantCulture,
          out var target)) return;
    RunOperation(seqKey, axis, "Move", target);
  }

  private void StopAxis(string axis)
  {
    if (!StopSeqKeys.TryGetValue(axis, out var seqKey)) return;
    RunOperation(seqKey, axis, "Stop");
  }

  private void StartJog(int direction)
  {
    if (!double.TryParse(JogSpeed, out var speed) || speed <= 0) return;
    _activeCts?.Cancel();
    _jogCts?.Cancel();
    _jogCts = new CancellationTokenSource();

    var velocity = speed * direction;
    _ = Task.Run(async () =>
    {
      try
      {
        await _core.Motion.JogAsync(SelectedAxis, velocity, _jogCts.Token)
          .ConfigureAwait(false);
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

  private double CurrentPosition(string axis) =>
    _core.AxisStateProvider.GetAxisState(axis).Position;

  private void RefreshAxisStatus()
  {
    try
    {
      foreach (var item in AxisStatuses)
      {
        var s = _core.AxisStateProvider.GetAxisState(item.AxisName);
        item.Position   = s.Position;
        item.IsMoving   = s.IsMoving;
        item.IsFault    = s.IsFault;
        item.IsHome     = s.IsHome;
        item.IsServoOn  = s.IsServoOn;
        item.IsLimitPos = s.IsLimitPos;
        item.IsLimitNeg = s.IsLimitNeg;
      }
    }
    catch (Exception ex)
    {
      LogStatus = $"Status refresh error: {ex.Message}";
    }
  }

  private void RunOperation(
    string seqKey, string axisLabel, string actionLabel, object? param = null)
  {
    _activeCts?.Cancel();
    _activeCts?.Dispose();
    var cts = new CancellationTokenSource();
    _activeCts = cts;

    _ = Task.Run(async () =>
    {
      try
      {
        var result = await _core.Sequences
          .RunAsync(seqKey, _core.Simulation, param, cts.Token)
          .ConfigureAwait(false);
        LogStatus = result.Status == SequenceStatus.Succeeded
          ? $"{axisLabel} {actionLabel} completed"
          : result.Error ?? $"{axisLabel} {actionLabel} failed";
      }
      catch (OperationCanceledException)
      {
        LogStatus = $"{axisLabel} {actionLabel} cancelled";
      }
      catch (Exception ex) { LogStatus = ex.Message; }
    });
  }

  #endregion

  #region Commands — Teach

  private RelayCommand? cmd_SaveTeach;
  public ICommand Cmd_SaveTeach =>
    cmd_SaveTeach ??= new RelayCommand(_ => SaveTeachValues());

  private RelayCommand? cmd_LoadTeach;
  public ICommand Cmd_LoadTeach =>
    cmd_LoadTeach ??= new RelayCommand(_ => LoadTeachValues());

  private RelayCommand? cmd_MoveToTeachPoint;
  public ICommand Cmd_MoveToTeachPoint =>
    cmd_MoveToTeachPoint ??= new RelayCommand(p =>
    {
      if (p is AxisTeachPointParam tp)
        MoveToTeachPoint(tp.Point, tp.AxisName);
    });

  private RelayCommand? cmd_TeachCurrentPos;
  public ICommand Cmd_TeachCurrentPos =>
    cmd_TeachCurrentPos ??= new RelayCommand(p =>
    {
      if (p is AxisTeachPointParam tp)
        TeachCurrentPosition(tp.Point, tp.AxisName);
    });

  #endregion

  #region Commands — Manual (Move/Stop/Servo/Home)

  private RelayCommand? cmd_MoveU;
  public ICommand Cmd_MoveU => cmd_MoveU ??= new RelayCommand(_ => MoveAxis(AxisDefine.U));

  private RelayCommand? cmd_MoveV;
  public ICommand Cmd_MoveV => cmd_MoveV ??= new RelayCommand(_ => MoveAxis(AxisDefine.V));

  private RelayCommand? cmd_MoveW;
  public ICommand Cmd_MoveW => cmd_MoveW ??= new RelayCommand(_ => MoveAxis(AxisDefine.W));

  private RelayCommand? cmd_MoveZLoad;
  public ICommand Cmd_MoveZLoad =>
    cmd_MoveZLoad ??= new RelayCommand(_ => MoveAxis(AxisDefine.ZLower));

  private RelayCommand? cmd_MoveZBond;
  public ICommand Cmd_MoveZBond =>
    cmd_MoveZBond ??= new RelayCommand(_ => MoveAxis(AxisDefine.ZUpper));

  private RelayCommand? cmd_StopU;
  public ICommand Cmd_StopU => cmd_StopU ??= new RelayCommand(_ => StopAxis(AxisDefine.U));
  private RelayCommand? cmd_StopV;
  public ICommand Cmd_StopV => cmd_StopV ??= new RelayCommand(_ => StopAxis(AxisDefine.V));
  private RelayCommand? cmd_StopW;
  public ICommand Cmd_StopW => cmd_StopW ??= new RelayCommand(_ => StopAxis(AxisDefine.W));
  private RelayCommand? cmd_StopZLoad;
  public ICommand Cmd_StopZLoad =>
    cmd_StopZLoad ??= new RelayCommand(_ => StopAxis(AxisDefine.ZLower));
  private RelayCommand? cmd_StopZBond;
  public ICommand Cmd_StopZBond =>
    cmd_StopZBond ??= new RelayCommand(_ => StopAxis(AxisDefine.ZUpper));

  private RelayCommand? cmd_ServoOnU;
  public ICommand Cmd_ServoOnU => cmd_ServoOnU ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_U_ServoOn, "U", "Servo On"));
  private RelayCommand? cmd_ServoOffU;
  public ICommand Cmd_ServoOffU => cmd_ServoOffU ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_U_ServoOff, "U", "Servo Off"));
  private RelayCommand? cmd_HomeU;
  public ICommand Cmd_HomeU => cmd_HomeU ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_U_Home, "U", "Home"));

  private RelayCommand? cmd_ServoOnV;
  public ICommand Cmd_ServoOnV => cmd_ServoOnV ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_V_ServoOn, "V", "Servo On"));
  private RelayCommand? cmd_ServoOffV;
  public ICommand Cmd_ServoOffV => cmd_ServoOffV ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_V_ServoOff, "V", "Servo Off"));
  private RelayCommand? cmd_HomeV;
  public ICommand Cmd_HomeV => cmd_HomeV ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_V_Home, "V", "Home"));

  private RelayCommand? cmd_ServoOnW;
  public ICommand Cmd_ServoOnW => cmd_ServoOnW ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_W_ServoOn, "W", "Servo On"));
  private RelayCommand? cmd_ServoOffW;
  public ICommand Cmd_ServoOffW => cmd_ServoOffW ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_W_ServoOff, "W", "Servo Off"));
  private RelayCommand? cmd_HomeW;
  public ICommand Cmd_HomeW => cmd_HomeW ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_W_Home, "W", "Home"));

  private RelayCommand? cmd_ServoOnZLoad;
  public ICommand Cmd_ServoOnZLoad => cmd_ServoOnZLoad ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_ServoOn, "ZLoad", "Servo On"));
  private RelayCommand? cmd_ServoOffZLoad;
  public ICommand Cmd_ServoOffZLoad => cmd_ServoOffZLoad ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_ServoOff, "ZLoad", "Servo Off"));
  private RelayCommand? cmd_HomeZLoad;
  public ICommand Cmd_HomeZLoad => cmd_HomeZLoad ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZLower_Home, "ZLoad", "Home"));

  private RelayCommand? cmd_ServoOnZBond;
  public ICommand Cmd_ServoOnZBond => cmd_ServoOnZBond ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOn, "ZBond", "Servo On"));
  private RelayCommand? cmd_ServoOffZBond;
  public ICommand Cmd_ServoOffZBond => cmd_ServoOffZBond ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_ServoOff, "ZBond", "Servo Off"));
  private RelayCommand? cmd_HomeZBond;
  public ICommand Cmd_HomeZBond => cmd_HomeZBond ??= new RelayCommand(
    _ => RunOperation(TcdSequenceKeys.Manual_Motor_ZUpper_Home, "ZBond", "Home"));

  private RelayCommand? cmd_JogPlusDown;
  public ICommand Cmd_JogPlusDown =>
    cmd_JogPlusDown ??= new RelayCommand(_ => StartJog(+1));
  private RelayCommand? cmd_JogPlusUp;
  public ICommand Cmd_JogPlusUp =>
    cmd_JogPlusUp ??= new RelayCommand(_ => StopJog());
  private RelayCommand? cmd_JogMinusDown;
  public ICommand Cmd_JogMinusDown =>
    cmd_JogMinusDown ??= new RelayCommand(_ => StartJog(-1));
  private RelayCommand? cmd_JogMinusUp;
  public ICommand Cmd_JogMinusUp =>
    cmd_JogMinusUp ??= new RelayCommand(_ => StopJog());

  private RelayCommand? cmd_StopAllMotors;
  public ICommand Cmd_StopAllMotors =>
    cmd_StopAllMotors ??= new RelayCommand(_ => StopAllMotors());

  #endregion

  // ── Inner types ─────────────────────────────────────────────────────────

  public sealed class AxisStatusItem : NotifyPropertyChangedBase
  {
    private string _axisName = "";
    private double _position;
    private bool _isServoOn, _isHome, _isMoving, _isFault, _isLimitPos, _isLimitNeg;

    public string AxisName  { get => _axisName;   set => Set(ref _axisName, value); }
    public double Position  { get => _position;   set => Set(ref _position, value); }
    public bool IsServoOn   { get => _isServoOn;  set => Set(ref _isServoOn, value); }
    public bool IsHome      { get => _isHome;     set => Set(ref _isHome, value); }
    public bool IsMoving    { get => _isMoving;   set => Set(ref _isMoving, value); }
    public bool IsFault     { get => _isFault;    set => Set(ref _isFault, value); }
    public bool IsLimitPos  { get => _isLimitPos; set => Set(ref _isLimitPos, value); }
    public bool IsLimitNeg  { get => _isLimitNeg; set => Set(ref _isLimitNeg, value); }
  }

  public sealed class AxisTeachPoint : NotifyPropertyChangedBase
  {
    private string _name = "";
    private double _position;
    public string Name     { get => _name;     set => Set(ref _name, value); }
    public double Position { get => _position; set => Set(ref _position, value); }
  }

  public sealed class AxisTeachGroup : NotifyPropertyChangedBase
  {
    private string _axisName    = "";
    private string _displayName = "";
    public string AxisName    { get => _axisName;    set => Set(ref _axisName, value); }
    public string DisplayName { get => _displayName; set => Set(ref _displayName, value); }
    public ObservableCollection<AxisTeachPoint> Points { get; } = new();
  }

  /// <summary>CommandParameter: 특정 축의 특정 점을 식별.</summary>
  public sealed class AxisTeachPointParam
  {
    public string AxisName { get; init; } = "";
    public AxisTeachPoint? Point { get; init; }
  }
}
