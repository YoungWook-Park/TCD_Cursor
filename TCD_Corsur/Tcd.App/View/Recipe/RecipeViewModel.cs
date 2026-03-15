using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;

namespace Tcd.App;

public sealed class RecipeViewModel : NotifyPropertyChangedBase
{
    private readonly MainCore _core = MainCore.Instance;

    public RecipeViewModel()
    {
        Reload();
    }

    public string RecipesDirectory => _core.RecipeRepository.RecipesDirectory;

    public ObservableCollection<string> RecipeNames { get; } = new();

    private string? _selectedRecipeName;
    public string? SelectedRecipeName
    {
        get => _selectedRecipeName;
        set
        {
            if (!Set(ref _selectedRecipeName, value)) return;
            LoadSelected();
        }
    }

    private string _status = "";
    public string Status { get => _status; private set => Set(ref _status, value); }

    // Editor fields (simple)
    private string _editName = "Default";
    public string EditName { get => _editName; set => Set(ref _editName, value); }

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

    private TcdRecipe? _loaded;

    private RelayCommand? cmd_Reload;
    public ICommand Cmd_Reload => cmd_Reload ??= new RelayCommand(_ => CmdReload());

    private RelayCommand? cmd_New;
    public ICommand Cmd_New => cmd_New ??= new RelayCommand(_ => CmdNew());

    private RelayCommand? cmd_Save;
    public ICommand Cmd_Save => cmd_Save ??= new RelayCommand(_ => CmdSave());

    private RelayCommand? cmd_SaveAs;
    public ICommand Cmd_SaveAs => cmd_SaveAs ??= new RelayCommand(_ => CmdSaveAs());

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
        U = _loaded.GetAxis("U").ToString("0.###");
        V = _loaded.GetAxis("V").ToString("0.###");
        W = _loaded.GetAxis("W").ToString("0.###");
        ZLoad = _loaded.GetAxis("Z_Load").ToString("0.###");
        ZBond = _loaded.GetAxis("Z_Bond").ToString("0.###");

        Status = $"Editing: {_loaded.Name}";
    }

    private void CmdReload()
    {
        try
        {
            Reload();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private void CmdNew()
    {
        _loaded = new TcdRecipe { Name = "NewRecipe" };
        _core.Recipes.Current = _loaded;

        EditName = _loaded.Name;
        U = "0";
        V = "0";
        W = "0";
        ZLoad = "0";
        ZBond = "100";
        Status = "New recipe (not saved).";
    }

    private void CmdSave()
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
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private void CmdSaveAs()
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
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private TcdRecipe BuildFromEditor()
    {
        var r = _loaded ?? new TcdRecipe();
        r.Name = string.IsNullOrWhiteSpace(EditName) ? "Recipe" : EditName.Trim();

        r.SetAxis("U", Parse(U));
        r.SetAxis("V", Parse(V));
        r.SetAxis("W", Parse(W));
        r.SetAxis("Z_Load", Parse(ZLoad));
        r.SetAxis("Z_Bond", Parse(ZBond));
        return r;
    }

    private static double Parse(string s)
        => double.TryParse(s, out var v) ? v : 0;

    // (no-op)
}

