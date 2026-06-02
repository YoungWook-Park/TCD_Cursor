using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Mvvm;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App;

public sealed class Teach_AxisViewModel : NotifyPropertyChangedBase
{
  #region Fields

  private readonly MainCore _core = MainCore.Instance;
  private CancellationTokenSource? _activeCts;
  private CancellationTokenSource? _jogCts;

  private string _logStatus = "";
  private string _incDelta  = "1.000";
  private string _incSpeed  = "10";
  private string _jogSpeed  = "10";
  private bool   _isInching;

  private static readonly IReadOnlyDictionary<string, string> AbsMoveSeqKeys =
    new Dictionary<string, string>
    {
      [AxisDefine.U]      = TcdSequenceKeys.Manual_Motor_U_AbsMove,
      [AxisDefine.V]      = TcdSequenceKeys.Manual_Motor_V_AbsMove,
      [AxisDefine.W]      = TcdSequenceKeys.Manual_Motor_W_AbsMove,
      [AxisDefine.ZLower] = TcdSequenceKeys.Manual_Motor_ZLower_AbsMove,
      [AxisDefine.ZUpper] = TcdSequenceKeys.Manual_Motor_ZUpper_AbsMove,
    };

  #endregion

  #region Constructor

  public Teach_AxisViewModel(
    string axisName,
    string displayName,
    IEnumerable<string> keys,
    ObservableCollection<AxisStatusItem> sharedStatuses)
  {
    AxisName     = axisName;
    DisplayName  = displayName;
    AxisStatuses = sharedStatuses;
    foreach (var k in keys)
      Points.Add(new TeachPoint { Name = k });
    LoadFromRecipe();
  }

  #endregion

  #region Properties

  public string AxisName    { get; }
  public string DisplayName { get; }

  public ObservableCollection<TeachPoint> Points { get; } = new();
  public ObservableCollection<AxisStatusItem> AxisStatuses { get; }

  public string LogStatus
  {
    get => _logStatus;
    private set => Set(ref _logStatus, value);
  }

  public string IncDelta
  {
    get => _incDelta;
    set => Set(ref _incDelta, value);
  }

  public string IncSpeed
  {
    get => _incSpeed;
    set => Set(ref _incSpeed, value);
  }

  public string JogSpeed
  {
    get => _jogSpeed;
    set => Set(ref _jogSpeed, value);
  }

  public bool IsInching
  {
    get => _isInching;
    set => Set(ref _isInching, value);
  }

  #endregion

  #region Recipe

  public void LoadFromRecipe()
  {
    var recipe = _core.Recipes.Current;
    if (recipe == null) return;
    foreach (var pt in Points)
    {
      pt.Position = recipe.GetNamedPosition(AxisName, pt.Name);
      var p = recipe.GetNamedParams(AxisName, pt.Name);
      pt.Speed = p.Speed;
      pt.Accel = p.Accel;
      pt.Decel = p.Decel;
    }
  }

  private void SaveToRecipe()
  {
    var recipe = _core.Recipes.Current ?? new TcdRecipe();
    foreach (var pt in Points)
    {
      recipe.SetNamedPosition(AxisName, pt.Name, pt.Position);
      recipe.SetNamedParams(AxisName, pt.Name,
        new AxisPositionParams
          { Speed = pt.Speed, Accel = pt.Accel, Decel = pt.Decel });
    }
    _core.RecipeRepository.Save(recipe);
    _core.Recipes.Current = recipe;
    LogStatus = $"{AxisName}: Teach positions saved.";
  }

  private RelayCommand? cmd_Save;
  public ICommand Cmd_Save =>
    cmd_Save ??= new RelayCommand(_ => SaveToRecipe());

  private RelayCommand? cmd_Revert;
  public ICommand Cmd_Revert =>
    cmd_Revert ??= new RelayCommand(_ =>
    {
      LoadFromRecipe();
      LogStatus = $"{AxisName}: Reverted to saved values.";
    });

  #endregion

  #region Teach Point Commands

  private RelayCommand? cmd_MoveToPoint;
  public ICommand Cmd_MoveToPoint =>
    cmd_MoveToPoint ??= new RelayCommand(p =>
    {
      if (p is TeachPoint pt) MoveToPoint(pt);
    });

  private RelayCommand? cmd_TeachCurrentPos;
  public ICommand Cmd_TeachCurrentPos =>
    cmd_TeachCurrentPos ??= new RelayCommand(p =>
    {
      if (p is TeachPoint pt) CaptureCurrentPos(pt);
    });

  private void MoveToPoint(TeachPoint pt)
  {
    if (!AbsMoveSeqKeys.TryGetValue(AxisName, out var seqKey)) return;
    RunOperation(seqKey, $"→ {pt.Name}", pt.Position);
  }

  private void CaptureCurrentPos(TeachPoint pt)
  {
    pt.Position = _core.AxisStateProvider.GetAxisState(AxisName).Position;
    LogStatus = $"Taught {AxisName}.{pt.Name} = {pt.Position:0.###}";
  }

  #endregion

  #region INC Move

  private RelayCommand? cmd_IncMove;
  public ICommand Cmd_IncMove =>
    cmd_IncMove ??= new RelayCommand(_ => ExecuteIncMove());

  private bool TryParseDelta(out double delta) =>
    double.TryParse(IncDelta, NumberStyles.Float,
      CultureInfo.InvariantCulture, out delta);

  private void ExecuteIncMove()
  {
    if (TryParseDelta(out var delta)) RunIncMove(delta);
  }

  private void RunIncMove(double delta)
  {
    _activeCts?.Cancel();
    _activeCts?.Dispose();
    var cts = new CancellationTokenSource();
    _activeCts = cts;
    _ = Task.Run(async () =>
    {
      try
      {
        await _core.Motion.IncMoveAsync(AxisName, delta, cts.Token)
          .ConfigureAwait(false);
        LogStatus = $"{AxisName} IncMove {delta:+0.###;-0.###} done";
      }
      catch (OperationCanceledException) { }
      catch (Exception ex) { LogStatus = ex.Message; }
    });
  }

  #endregion

  #region Jog

  private RelayCommand? cmd_JogPlusDown;
  public ICommand Cmd_JogPlusDown =>
    cmd_JogPlusDown ??= new RelayCommand(_ => OnJogDown(+1));

  private RelayCommand? cmd_JogPlusUp;
  public ICommand Cmd_JogPlusUp =>
    cmd_JogPlusUp ??= new RelayCommand(_ => { if (!_isInching) StopJog(); });

  private RelayCommand? cmd_JogMinusDown;
  public ICommand Cmd_JogMinusDown =>
    cmd_JogMinusDown ??= new RelayCommand(_ => OnJogDown(-1));

  private RelayCommand? cmd_JogMinusUp;
  public ICommand Cmd_JogMinusUp =>
    cmd_JogMinusUp ??= new RelayCommand(_ => { if (!_isInching) StopJog(); });

  private RelayCommand? cmd_Stop;
  public ICommand Cmd_Stop =>
    cmd_Stop ??= new RelayCommand(_ =>
    {
      _activeCts?.Cancel();
      StopJog();
    });

  private void OnJogDown(int direction)
  {
    if (_isInching)
    {
      if (TryParseDelta(out var delta))
        RunIncMove(Math.Abs(delta) * direction);
    }
    else
    {
      StartJog(direction);
    }
  }

  private void StartJog(int direction)
  {
    if (!double.TryParse(JogSpeed, NumberStyles.Float,
          CultureInfo.InvariantCulture, out var speed) || speed <= 0)
      return;

    _activeCts?.Cancel();
    _jogCts?.Cancel();
    _jogCts = new CancellationTokenSource();

    var velocity = speed * direction;
    _ = Task.Run(async () =>
    {
      try
      {
        await _core.Motion.JogAsync(AxisName, velocity, _jogCts.Token)
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
    _ = _core.Motion.StopAsync(AxisName, CancellationToken.None);
  }

  #endregion

  #region RunOperation

  private void RunOperation(
    string seqKey, string actionLabel, object? param = null)
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
          ? $"{AxisName} {actionLabel} completed"
          : result.Error ?? $"{AxisName} {actionLabel} failed";
      }
      catch (OperationCanceledException)
      {
        LogStatus = $"{AxisName} {actionLabel} cancelled";
      }
      catch (Exception ex) { LogStatus = ex.Message; }
    });
  }

  #endregion

  // ── Inner types ─────────────────────────────────────────────────────────

  public sealed class TeachPoint : NotifyPropertyChangedBase
  {
    private string _name     = "";
    private double _position;
    private double _speed    = 100;
    private double _accel    = 1000;
    private double _decel    = 1000;

    public string Name     { get => _name;     set => Set(ref _name, value); }
    public double Position { get => _position; set => Set(ref _position, value); }
    public double Speed    { get => _speed;    set => Set(ref _speed, value); }
    public double Accel    { get => _accel;    set => Set(ref _accel, value); }
    public double Decel    { get => _decel;    set => Set(ref _decel, value); }
  }

  public sealed class AxisStatusItem : NotifyPropertyChangedBase
  {
    private string _axisName = "";
    private double _position;
    private bool _isServoOn, _isHome, _isMoving, _isFault,
                 _isLimitPos, _isLimitNeg;

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
