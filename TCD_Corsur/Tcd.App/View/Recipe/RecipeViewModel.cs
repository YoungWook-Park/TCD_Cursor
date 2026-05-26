using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Mvvm;

namespace Tcd.App;

// ── 로봇 속도 편집 행 (Robot 탭 ItemsControl 에서 1행) ────────────────────
public sealed class RobotVelocityEditRow : NotifyPropertyChangedBase
{
    private string _velocity;

    public string PositionName { get; init; }

    public string Velocity
    {
        get => _velocity;
        set => Set(ref _velocity, value);
    }

    public int VelocityInt =>
        int.TryParse(Velocity, out var v) ? Math.Clamp(v, 1, 100) : 50;

    public RobotVelocityEditRow(string positionName, int velocity)
    {
        PositionName = positionName;
        _velocity    = velocity.ToString();
    }
}

public sealed class RecipeViewModel : NotifyPropertyChangedBase
{
    #region Variable

    private readonly MainCore _core = MainCore.Instance;
    private string? _selectedRecipeName;
    private string _status = "";
    private string _editName = "Default";
    private string _u = "0";
    private string _v = "0";
    private string _w = "0";
    private string _zLoad = "0";
    private string _zBond = "100";
    private string _motionVelocity = "100";
    private string _motionAcc = "1000";
    private string _motionDec = "1000";
    private string _motionJerk = "1000";
    private TcdRecipe? _loaded;

    #endregion

    #region Constructor

    public RecipeViewModel()
    {
        RobotVelocityRows = new ObservableCollection<RobotVelocityEditRow>
        {
            new(RobotPositionName.Home,
                RobotVelocityDefault.Home),
            new(RobotPositionName.Ready,
                RobotVelocityDefault.Ready),
            new(RobotPositionName.S1_PickupWait,
                RobotVelocityDefault.S1_PickupWait),
            new(RobotPositionName.S1_Pick,
                RobotVelocityDefault.S1_Pick),
            new(RobotPositionName.S2_PickupWait,
                RobotVelocityDefault.S2_PickupWait),
            new(RobotPositionName.S2_Pick,
                RobotVelocityDefault.S2_Pick),
            new(RobotPositionName.UpperChamber_PickupWait,
                RobotVelocityDefault.UpperChamber_PickupWait),
            new(RobotPositionName.UpperChamber_Pick,
                RobotVelocityDefault.UpperChamber_Pick),
            new(RobotPositionName.LowerChamber_PickupWait,
                RobotVelocityDefault.LowerChamber_PickupWait),
            new(RobotPositionName.LowerChamber_Pick,
                RobotVelocityDefault.LowerChamber_Pick),
            new(RobotPositionName.Peel,
                RobotVelocityDefault.Peel),
        };

        Reload();
    }

    #endregion

    #region Properties — Recipe List

    public string RecipesDirectory => _core.RecipeRepository.RecipesDirectory;
    public ObservableCollection<string> RecipeNames { get; } = new();

    public string? SelectedRecipeName
    {
        get => _selectedRecipeName;
        set
        {
            if (!Set(ref _selectedRecipeName, value)) return;
            LoadSelected();
        }
    }

    public string Status { get => _status; private set => Set(ref _status, value); }

    #endregion

    #region Properties — Motor Tab

    public string EditName      { get => _editName;       set => Set(ref _editName, value); }
    public string U             { get => _u;              set => Set(ref _u, value); }
    public string V             { get => _v;              set => Set(ref _v, value); }
    public string W             { get => _w;              set => Set(ref _w, value); }
    public string ZLoad         { get => _zLoad;          set => Set(ref _zLoad, value); }
    public string ZBond         { get => _zBond;          set => Set(ref _zBond, value); }
    public string MotionVelocity { get => _motionVelocity; set => Set(ref _motionVelocity, value); }
    public string MotionAcc     { get => _motionAcc;      set => Set(ref _motionAcc, value); }
    public string MotionDec     { get => _motionDec;      set => Set(ref _motionDec, value); }
    public string MotionJerk    { get => _motionJerk;     set => Set(ref _motionJerk, value); }

    #endregion

    #region Properties — Robot Tab

    /// <summary>Robot 탭: 포지션별 속도 편집 행 (ItemsControl 바인딩)</summary>
    public ObservableCollection<RobotVelocityEditRow> RobotVelocityRows { get; }

    #endregion

    #region Function

    public void Reload()
    {
        RecipeNames.Clear();
        foreach (var name in _core.RecipeRepository.ListRecipeNames())
            RecipeNames.Add(name);
        SelectedRecipeName ??= RecipeNames.FirstOrDefault();
        Status = $"Loaded {RecipeNames.Count} recipe(s).";
    }

    public TcdRecipe? GetCurrentRecipe()
    {
        if (_loaded == null) LoadSelected();
        return _loaded;
    }

    private void LoadSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedRecipeName)) return;
        _loaded = _core.LoadRecipe(SelectedRecipeName);

        // Motor 탭
        EditName      = _loaded.Name;
        U             = _loaded.GetAxis(AxisDefine.U).ToString("0.###");
        V             = _loaded.GetAxis(AxisDefine.V).ToString("0.###");
        W             = _loaded.GetAxis(AxisDefine.W).ToString("0.###");
        ZLoad         = _loaded.GetAxis(AxisDefine.ZLower).ToString("0.###");
        ZBond         = _loaded.GetAxis(AxisDefine.ZUpper).ToString("0.###");
        MotionVelocity = _loaded.MotionVelocity.ToString("0.###");
        MotionAcc     = _loaded.MotionAcc.ToString("0.###");
        MotionDec     = _loaded.MotionDec.ToString("0.###");
        MotionJerk    = _loaded.MotionJerk.ToString("0.###");

        // Robot 탭 — 포지션 속도
        foreach (var row in RobotVelocityRows)
            row.Velocity = _loaded.GetRobotVelocity(row.PositionName).ToString();

        Status = $"Editing: {_loaded.Name}";
    }

    private TcdRecipe BuildFromEditor()
    {
        var r = _loaded ?? new TcdRecipe();

        // Motor 탭
        r.Name = string.IsNullOrWhiteSpace(EditName) ? "Recipe" : EditName.Trim();
        r.SetAxis(AxisDefine.U,      ParseDouble(U));
        r.SetAxis(AxisDefine.V,      ParseDouble(V));
        r.SetAxis(AxisDefine.W,      ParseDouble(W));
        r.SetAxis(AxisDefine.ZLower, ParseDouble(ZLoad));
        r.SetAxis(AxisDefine.ZUpper, ParseDouble(ZBond));
        r.MotionVelocity = ParseDouble(MotionVelocity);
        r.MotionAcc      = ParseDouble(MotionAcc);
        r.MotionDec      = ParseDouble(MotionDec);
        r.MotionJerk     = ParseDouble(MotionJerk);

        // Robot 탭 — 포지션 속도
        foreach (var row in RobotVelocityRows)
            r.RobotVelocity[row.PositionName] = row.VelocityInt;

        return r;
    }

    private static double ParseDouble(string s) =>
        double.TryParse(s, out var v) ? v : 0;

    #endregion

    #region Commands

    private RelayCommand? cmd_Reload;
    public ICommand Cmd_Reload => cmd_Reload ??= new RelayCommand(OnCmd_Reload);

    private RelayCommand? cmd_New;
    public ICommand Cmd_New => cmd_New ??= new RelayCommand(OnCmd_New);

    private RelayCommand? cmd_Save;
    public ICommand Cmd_Save => cmd_Save ??= new RelayCommand(OnCmd_Save);

    private RelayCommand? cmd_SaveAs;
    public ICommand Cmd_SaveAs => cmd_SaveAs ??= new RelayCommand(OnCmd_SaveAs);

    private void OnCmd_Reload(object? _)
    {
        try { Reload(); }
        catch (Exception ex) { Status = ex.Message; }
    }

    private void OnCmd_New(object? _)
    {
        try
        {
            _loaded = new TcdRecipe { ModelName = "Default", Name = "NewRecipe" };
            _core.Recipes.Current = _loaded;

            EditName = _loaded.Name;
            U = "0"; V = "0"; W = "0"; ZLoad = "0"; ZBond = "100";
            MotionVelocity = "100"; MotionAcc = "1000";
            MotionDec = "1000"; MotionJerk = "1000";

            foreach (var row in RobotVelocityRows)
                row.Velocity = _loaded.GetRobotVelocity(row.PositionName).ToString();

            Status = "New recipe (not saved).";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    private void OnCmd_Save(object? _)
    {
        try
        {
            var r = BuildFromEditor();
            _core.SaveRecipe(r);
            _loaded = r;
            Reload();
            SelectedRecipeName = r.Name;
            Status = $"Saved: {r.Name}";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    private void OnCmd_SaveAs(object? _)
    {
        try
        {
            var r = BuildFromEditor();
            if (string.IsNullOrWhiteSpace(r.Name) ||
                r.Name.Equals(_loaded?.Name, StringComparison.OrdinalIgnoreCase))
                r.Name = $"{r.Name}_Copy";
            _core.SaveRecipe(r);
            _loaded = r;
            Reload();
            SelectedRecipeName = r.Name;
            Status = $"Saved As: {r.Name}";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    #endregion
}
