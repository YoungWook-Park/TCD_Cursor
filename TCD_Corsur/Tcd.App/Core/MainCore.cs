using System.IO;
using Tcd.App.Sequences.Manual;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Core;

/// <summary>
/// Single, easy-to-find composition root for the whole application.
/// Keeps shared instances (settings, recipes, devices/simulation, sequences, alarms, log).
/// </summary>
public sealed class MainCore
{
    private static readonly Lazy<MainCore> _lazy = new(() => new MainCore());
    public static MainCore Instance => _lazy.Value;

    private MainCore() { }

    public bool IsInitialized { get; private set; }

    public AppSettings Settings { get; private set; } = new();
    public RecipeStore Recipes { get; private set; } = new();
    public IRecipeRepository RecipeRepository { get; private set; } = null!;

    public AlarmManager Alarms { get; private set; } = new();
    public TcdSimulation Simulation { get; private set; } = null!;
    public SequenceManager Sequences { get; private set; } = null!;
    public IMotionService Motion { get; private set; } = null!;
    public ILogWriter Log { get; private set; } = null!;

    private LogWriter? _logWriter;

    public void Initialize()
    {
        if (IsInitialized) return;

        // 1) Load settings/recipes
        Settings = AppSettings.CreateDefaults();
        RecipeRepository = new JsonRecipeRepository(RecipePaths.DefaultRecipesDirectory());
        Recipes = RecipeStore.LoadOrCreateDefaults(RecipeRepository);

        // 2) Create simulation/devices
        Simulation = new TcdSimulation();
        Motion = CreateMotionService();

        // 3) Centralize alarms: make sure simulation raises into the same AlarmManager
        // (TcdSimulation currently creates its own AlarmManager; keep a single reference for the app)
        Alarms = (AlarmManager)Simulation.Alarms;

        // 4) LogWriter (single instance, async queue + background consumer)
        var logDir = Path.Combine(Path.GetTempPath(), "Tcd", "Logs");
        var sink = new FileLogSink(logDir, "tcd", LogLevel.Debug);
        _logWriter = new LogWriter(sink, boundedCapacity: 4096, batchSize: 100, batchWaitMs: 500);
        _logWriter.Start();
        Log = _logWriter;

        // 5) Register all sequences once (Simulator 기반 + App Manual 시퀀스)
        Sequences = TcdSequenceRegistry.Build(Simulation, Motion);
        Sequences.Register(new Manual_Axis0_U_AbsMove());
        Sequences.Register(new Axis0StopSequence());

        IsInitialized = true;
    }

    private IMotionService CreateMotionService()
    {
        if (Settings.UseSpiiPlus)
        {
            // 실제 SPIIPLUS 제어기와 연결
            return new Spii.SpiiPlusMotionService(Settings.SpiiIpAddress);
        }

        // 기본은 시뮬레이터 모션 사용
        var proxy = new AppSettingsProxy(Settings.AxisMoveTimeout);
        return new SimMotionService(Simulation, proxy);
    }
}

public sealed class AppSettings
{
    public TimeSpan StageLoadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RobotMoveTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan AxisMoveTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public bool UseSpiiPlus { get; set; } = true;
    public string SpiiIpAddress { get; set; } = "10.0.0.100";

    public static AppSettings CreateDefaults() => new();
}

public sealed class RecipeStore
{
    public IReadOnlyList<TcdRecipe> Items { get; private set; } = Array.Empty<TcdRecipe>();

    public TcdRecipe? Current { get; set; }

    public static RecipeStore LoadOrCreateDefaults(IRecipeRepository repo)
    {
        var store = new RecipeStore();
        var names = repo.ListRecipeNames();
        if (names.Count == 0)
        {
            var def = new TcdRecipe { Name = "Default" };
            repo.Save(def);
            names = repo.ListRecipeNames();
        }

        store.Items = names.Select(repo.Load).ToArray();
        store.Current = store.Items.FirstOrDefault();
        return store;
    }
}

