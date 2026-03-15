using System.IO;
using System.Linq;
using Tcd.App.Sequences.Manual.Motor_U;
using Tcd.App.Sequences.Manual.Motor_V;
using Tcd.App.Sequences.Manual.Motor_W;
using Tcd.App.Sequences.Manual.Motor_ZLower;
using Tcd.App.Sequences.Manual.Motor_ZUpper;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

using LogContext = Tcd.Core.Logging.LogContext;

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
    /// <summary>모션 상태(위치/이동/폴트/홈) 읽기 전용. 백그라운드 모니터가 갱신한 캐시만 참조.</summary>
    public IAxisStateProvider AxisStateProvider { get; private set; } = null!;
    public ILogWriter Log { get; private set; } = null!;
    /// <summary>현재 시퀀스 실행용 로그 컨텍스트. 시퀀스 시작 시 설정, 헬퍼에서 참조.</summary>
    public LogContext LogContext { get; set; }

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
        AxisStateProvider = (IAxisStateProvider)Motion;

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
        RegisterManualMotorSequences();
        Sequences.Register(new DelegateSequence(TcdSequenceKeys.Sequence_Init, "Init", (ctx, p, ct) =>
        {
            // 레시피(전역)는 이미 MainCore.Recipes.Current. 모션 서비스는 이동 시 참조.
            return Task.CompletedTask;
        }));

        IsInitialized = true;
    }

    private void RegisterManualMotorSequences()
    {
        // Motor_U
        Sequences.Register(new ManualMotorUAbsMoveSequence());
        Sequences.Register(new ManualMotorUIncMoveSequence());
        Sequences.Register(new ManualMotorUJogMoveSequence());
        Sequences.Register(new ManualMotorUStopSequence());
        Sequences.Register(new ManualMotorUHomeSequence());
        Sequences.Register(new ManualMotorUFaultResetSequence());
        Sequences.Register(new ManualMotorUServoOnSequence());
        Sequences.Register(new ManualMotorUServoOffSequence());
        // Motor_V
        Sequences.Register(new ManualMotorVAbsMoveSequence());
        Sequences.Register(new ManualMotorVIncMoveSequence());
        Sequences.Register(new ManualMotorVJogMoveSequence());
        Sequences.Register(new ManualMotorVStopSequence());
        Sequences.Register(new ManualMotorVHomeSequence());
        Sequences.Register(new ManualMotorVFaultResetSequence());
        Sequences.Register(new ManualMotorVServoOnSequence());
        Sequences.Register(new ManualMotorVServoOffSequence());
        // Motor_W
        Sequences.Register(new ManualMotorWAbsMoveSequence());
        Sequences.Register(new ManualMotorWIncMoveSequence());
        Sequences.Register(new ManualMotorWJogMoveSequence());
        Sequences.Register(new ManualMotorWStopSequence());
        Sequences.Register(new ManualMotorWHomeSequence());
        Sequences.Register(new ManualMotorWFaultResetSequence());
        Sequences.Register(new ManualMotorWServoOnSequence());
        Sequences.Register(new ManualMotorWServoOffSequence());
        // Motor_ZLower
        Sequences.Register(new ManualMotorZLowerAbsMoveSequence());
        Sequences.Register(new ManualMotorZLowerIncMoveSequence());
        Sequences.Register(new ManualMotorZLowerJogMoveSequence());
        Sequences.Register(new ManualMotorZLowerStopSequence());
        Sequences.Register(new ManualMotorZLowerHomeSequence());
        Sequences.Register(new ManualMotorZLowerFaultResetSequence());
        Sequences.Register(new ManualMotorZLowerServoOnSequence());
        Sequences.Register(new ManualMotorZLowerServoOffSequence());
        // Motor_ZUpper
        Sequences.Register(new ManualMotorZUpperAbsMoveSequence());
        Sequences.Register(new ManualMotorZUpperIncMoveSequence());
        Sequences.Register(new ManualMotorZUpperJogMoveSequence());
        Sequences.Register(new ManualMotorZUpperStopSequence());
        Sequences.Register(new ManualMotorZUpperHomeSequence());
        Sequences.Register(new ManualMotorZUpperFaultResetSequence());
        Sequences.Register(new ManualMotorZUpperServoOnSequence());
        Sequences.Register(new ManualMotorZUpperServoOffSequence());
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
    private TcdRecipe? _current;

    /// <summary>모델 목록 (모델 → 레시피 계층)</summary>
    public IReadOnlyList<TcdModel> Models { get; private set; } = Array.Empty<TcdModel>();

    /// <summary>전체 레시피 플랫 목록 (하위 호환)</summary>
    public IReadOnlyList<TcdRecipe> Items { get; private set; } = Array.Empty<TcdRecipe>();

    /// <summary>현재 선택된 레시피. Core에서 참조.</summary>
    public TcdRecipe? Current
    {
        get => _current;
        set => SetCurrentRecipe(value);
    }

    /// <summary>현재 선택된 모델 (Current 레시피가 속한 모델, ModelName으로 매칭)</summary>
    public TcdModel? CurrentModel { get; private set; }

    public event EventHandler? CurrentChanged;

    /// <summary>현재 레시피 지정. 소속 모델을 CurrentModel로 설정.</summary>
    public void SetCurrentRecipe(TcdRecipe? recipe)
    {
        if (_current == recipe) return;
        _current = recipe;
        CurrentModel = recipe == null ? null : Models.FirstOrDefault(m => string.Equals(m.Name, recipe.ModelName, StringComparison.OrdinalIgnoreCase));
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }

    public static RecipeStore LoadOrCreateDefaults(IRecipeRepository repo)
    {
        var store = new RecipeStore();
        var names = repo.ListRecipeNames();
        if (names.Count == 0)
        {
            var def = new TcdRecipe { ModelName = "Default", Name = "Default" };
            repo.Save(def);
            names = repo.ListRecipeNames();
        }

        var allRecipes = names.Select(repo.Load).ToArray();
        store.Items = allRecipes;

        var byModel = allRecipes
            .GroupBy(r => string.IsNullOrWhiteSpace(r.ModelName) ? "Default" : r.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TcdModel { Name = g.Key, Recipes = g.ToList() })
            .ToArray();
        store.Models = byModel;

        var firstModel = byModel.FirstOrDefault();
        var firstRecipe = firstModel?.Recipes.FirstOrDefault() ?? allRecipes.FirstOrDefault();
        store.CurrentModel = firstModel;
        store._current = firstRecipe;
        return store;
    }
}

