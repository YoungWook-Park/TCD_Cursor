using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;

namespace Tcd.App;

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
        Reload();
    }

    #endregion

    #region Property

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
    public string EditName { get => _editName; set => Set(ref _editName, value); }
    public string U { get => _u; set => Set(ref _u, value); }
    public string V { get => _v; set => Set(ref _v, value); }
    public string W { get => _w; set => Set(ref _w, value); }
    public string ZLoad { get => _zLoad; set => Set(ref _zLoad, value); }
    public string ZBond { get => _zBond; set => Set(ref _zBond, value); }
    public string MotionVelocity { get => _motionVelocity; set => Set(ref _motionVelocity, value); }
    public string MotionAcc { get => _motionAcc; set => Set(ref _motionAcc, value); }
    public string MotionDec { get => _motionDec; set => Set(ref _motionDec, value); }
    public string MotionJerk { get => _motionJerk; set => Set(ref _motionJerk, value); }

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
        _loaded = _core.RecipeRepository.Load(SelectedRecipeName);
        _core.Recipes.Current = _loaded;

        EditName = _loaded.Name;
        U = _loaded.GetAxis(AxisDefine.U).ToString("0.###");
        V = _loaded.GetAxis(AxisDefine.V).ToString("0.###");
        W = _loaded.GetAxis(AxisDefine.W).ToString("0.###");
        ZLoad = _loaded.GetAxis(AxisDefine.ZLower).ToString("0.###");
        ZBond = _loaded.GetAxis(AxisDefine.ZUpper).ToString("0.###");
        MotionVelocity = _loaded.MotionVelocity.ToString("0.###");
        MotionAcc = _loaded.MotionAcc.ToString("0.###");
        MotionDec = _loaded.MotionDec.ToString("0.###");
        MotionJerk = _loaded.MotionJerk.ToString("0.###");
        Status = $"Editing: {_loaded.Name}";
    }

    private TcdRecipe BuildFromEditor()
    {
        var r = _loaded ?? new TcdRecipe();
        r.Name = string.IsNullOrWhiteSpace(EditName) ? "Recipe" : EditName.Trim();
        r.SetAxis(AxisDefine.U, Parse(U));
        r.SetAxis(AxisDefine.V, Parse(V));
        r.SetAxis(AxisDefine.W, Parse(W));
        r.SetAxis(AxisDefine.ZLower, Parse(ZLoad));
        r.SetAxis(AxisDefine.ZUpper, Parse(ZBond));
        r.MotionVelocity = Parse(MotionVelocity);
        r.MotionAcc = Parse(MotionAcc);
        r.MotionDec = Parse(MotionDec);
        r.MotionJerk = Parse(MotionJerk);
        return r;
    }

    private static double Parse(string s) => double.TryParse(s, out var v) ? v : 0;

    #endregion

    #region UI Function

    private RelayCommand? cmd_Reload;
    public ICommand Cmd_Reload => cmd_Reload ??= new RelayCommand(PerformCmd_Reload);

    private void PerformCmd_Reload(object? commandParameter)
    {
        try { Reload(); }
        catch (Exception ex) { Status = ex.Message; }
        finally { }
    }

    private RelayCommand? cmd_New;
    public ICommand Cmd_New => cmd_New ??= new RelayCommand(PerformCmd_New);

    private void PerformCmd_New(object? commandParameter)
    {
        try
        {
            _loaded = new TcdRecipe { ModelName = "Default", Name = "NewRecipe" };
            _core.Recipes.Current = _loaded;
            EditName = _loaded.Name;
            U = "0"; V = "0"; W = "0"; ZLoad = "0"; ZBond = "100";
            MotionVelocity = "100"; MotionAcc = "1000"; MotionDec = "1000"; MotionJerk = "1000";
            Status = "New recipe (not saved).";
        }
        catch (Exception ex) { Status = ex.Message; }
        finally { }
    }

    private RelayCommand? cmd_Save;
    public ICommand Cmd_Save => cmd_Save ??= new RelayCommand(_ => PerformCmd_Save(null));

    private void PerformCmd_Save(object? commandParameter)
    {
        try
        {
            var r = BuildFromEditor();
            _core.RecipeRepository.Save(r);
            _loaded = r;
            _core.Recipes.Current = _loaded;
            Reload();
            SelectedRecipeName = r.Name;
            Status = $"Saved: {r.Name}";
        }
        catch (Exception ex) { Status = ex.Message; }
        finally { }
    }

    private RelayCommand? cmd_SaveAs;
    public ICommand Cmd_SaveAs => cmd_SaveAs ??= new RelayCommand(PerformCmd_SaveAs);

    private void PerformCmd_SaveAs(object? commandParameter)
    {
        try
        {
            var r = BuildFromEditor();
            if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Equals(_loaded?.Name, StringComparison.OrdinalIgnoreCase))
                r.Name = $"{r.Name}_Copy";
            _core.RecipeRepository.Save(r);
            _loaded = r;
            _core.Recipes.Current = _loaded;
            Reload();
            SelectedRecipeName = r.Name;
            Status = $"Saved As: {r.Name}";
        }
        catch (Exception ex) { Status = ex.Message; }
        finally { }
    }

    #endregion
}
