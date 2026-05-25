using System.IO;
using System.Linq;
using Tcd.App.Devices;
using Tcd.App.Sequences.Auto;
using Tcd.App.Sequences.Manual;
using Tcd.App.Sequences.SemiAuto;
using Tcd.Core;
using Tcd.Core.Logging;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

using LogContext = Tcd.Core.Logging.LogContext;

namespace Tcd.App.Core;

/// <summary>
/// 애플리케이션 전체 공유 인스턴스의 단일 컴포지션 루트.
/// 설정, 레시피, 디바이스, 시퀀스, 알람, 로그를 보관.
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
    /// <summary>현재 시퀀스 실행용 로그 컨텍스트. 시퀀스 시작 시 설정.</summary>
    public LogContext LogContext { get; set; }
    /// <summary>TCP 로봇 시뮬레이터 클라이언트. ConnectAsync 호출 전까지 연결 안 됨.</summary>
    public IRobotDevice RobotDevice { get; private set; } = null!;
    /// <summary>TCP PLC 클라이언트. ConnectAsync 호출 전까지 연결 안 됨.</summary>
    public PlcTcpClient PlcDevice { get; private set; } = null!;

    private LogWriter? _logWriter;

    public void Initialize()
    {
        if (IsInitialized) return;

        // 1) 설정/레시피 로드
        Settings = AppSettings.CreateDefaults();
        RecipeRepository = new JsonRecipeRepository(RecipePaths.DefaultRecipesDirectory());
        Recipes = RecipeStore.LoadOrCreateDefaults(RecipeRepository);

        // 2) 디바이스 생성
        Simulation = new TcdSimulation();
        Motion = CreateMotionService();
        AxisStateProvider = (IAxisStateProvider)Motion;

        // 3) 알람 싱크 통일
        Alarms = (AlarmManager)Simulation.Alarms;

        // 4) 비동기 로그 라이터
        var logDir = Path.Combine(Path.GetTempPath(), "Tcd", "Logs");
        var sink = new FileLogSink(logDir, "tcd", LogLevel.Debug);
        _logWriter = new LogWriter(sink, boundedCapacity: 4096, batchSize: 100, batchWaitMs: 500);
        _logWriter.Start();
        Log = _logWriter;

        // 5) TCP 디바이스 인스턴스 생성 (연결은 ViewModel에서 수행)
        RobotDevice = new RobotTcpClient();
        PlcDevice   = new PlcTcpClient();

        // 6) 시퀀스 등록 (시뮬레이터 원자 시퀀스 + 수동/반자동/자동)
        Sequences = TcdSequenceRegistry.Build(Simulation, Motion);
        RegisterManualMotorSequences();
        RegisterSemiAutoAndAutoSequences();
        Sequences.Register(new DelegateSequence(TcdSequenceKeys.Sequence_Init, "Init",
            (ctx, p, ct) => Task.CompletedTask));

        IsInitialized = true;
    }

    private void RegisterManualMotorSequences()
    {
        Manual_AxisU.RegisterAll(Sequences);
        Manual_AxisV.RegisterAll(Sequences);
        Manual_AxisW.RegisterAll(Sequences);
        Manual_AxisZLower.RegisterAll(Sequences);
        Manual_AxisZUpper.RegisterAll(Sequences);
    }

    private void RegisterSemiAutoAndAutoSequences()
    {
        Sequences.Register(new SemiAutoLoadUpperFilmSequence(Sequences, Simulation));
        Sequences.Register(new SemiAutoLoadLowerFilmSequence(Sequences, Simulation));
        Sequences.Register(new SemiAutoAlignUVWSequence(Sequences, Simulation));
        Sequences.Register(new SemiAutoBondSequence(Sequences));
        Sequences.Register(new SemiAutoUnloadProductToStage2Sequence(Sequences));
        Sequences.Register(new AutoRunSequence(Sequences));
    }

    private IMotionService CreateMotionService()
    {
        if (Settings.UseSpiiPlus)
            return new Spii.SpiiPlusMotionService(Settings.SpiiIpAddress);

        var proxy = new AppSettingsProxy(Settings.AxisMoveTimeout);
        return new SimMotionService(Simulation, proxy);
    }
}

public sealed class AppSettings
{
    public TimeSpan StageLoadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RobotMoveTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan AxisMoveTimeout  { get; set; } = TimeSpan.FromSeconds(3);

    // ── SPiiPlus 모션 ──────────────────────────────────────────────────────
    public bool UseSpiiPlus  { get; set; } = false;
    public string SpiiIpAddress { get; set; } = "10.0.0.100";

    // ── TCP 로봇 시뮬레이터 ────────────────────────────────────────────────
    /// <summary>로봇 시뮬레이터 서버 주소 (기본: localhost)</summary>
    public string RobotSimHost { get; set; } = "127.0.0.1";
    /// <summary>로봇 시뮬레이터 서버 포트 (기본: 7001)</summary>
    public int    RobotSimPort { get; set; } = 7001;

    // ── TCP PLC 시뮬레이터 ─────────────────────────────────────────────────
    /// <summary>PLC 시뮬레이터 서버 주소 (기본: localhost)</summary>
    public string PlcSimHost { get; set; } = "127.0.0.1";
    /// <summary>PLC 시뮬레이터 서버 포트 (기본: 7002)</summary>
    public int    PlcSimPort { get; set; } = 7002;

    public static AppSettings CreateDefaults() => new();
}

public sealed class RecipeStore
{
    private TcdRecipe? _current;

    public IReadOnlyList<TcdModel> Models { get; private set; } = Array.Empty<TcdModel>();
    public IReadOnlyList<TcdRecipe> Items { get; private set; } = Array.Empty<TcdRecipe>();

    public TcdRecipe? Current
    {
        get => _current;
        set => SetCurrentRecipe(value);
    }

    public TcdModel? CurrentModel { get; private set; }

    public event EventHandler? CurrentChanged;

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
