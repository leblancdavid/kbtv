using Godot;
using KBTV.Managers;
using KBTV.Economy;
using KBTV.Persistence;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Dialogue;
using KBTV.UI;
using KBTV.Ads;
using KBTV.Screening;

namespace KBTV.Core;

    /// <summary>
    /// Root service provider that manages all game services as AutoInject providers.
    /// This replaces the ServiceRegistry global singleton approach.
    /// </summary>
    public partial class ServiceProviderRoot : Node,
    IProvide<GameStateManager>,
    IProvide<TimeManager>,
    IProvide<EconomyManager>,
    IProvide<ListenerManager>,
    IProvide<SaveManager>,
    IProvide<EventBus>,
    IProvide<ICallerRepository>,
    IProvide<IArcRepository>,
    IProvide<IDialoguePlayer>,
    IProvide<CallerGenerator>,
    IProvide<UIManager>,
    IProvide<AsyncBroadcastLoop>,
    IProvide<BroadcastCoordinator>,
    IProvide<GlobalTransitionManager>,
    IProvide<AdManager>,
    IProvide<ITranscriptRepository>,
    IProvide<IScreeningController>,
    IProvide<IGameStateManager>,
    IProvide<ITimeManager>
{
    public override void _Notification(int what) => this.Notify(what);

    // Provider properties - will be set in Initialize()
    public GameStateManager GameStateManager { get; private set; } = null!;
    public TimeManager TimeManager { get; private set; } = null!;
    public EconomyManager EconomyManager { get; private set; } = null!;
    public ListenerManager ListenerManager { get; private set; } = null!;
    public SaveManager SaveManager { get; private set; } = null!;
    public EventBus EventBus { get; private set; } = null!;
    public ICallerRepository CallerRepository { get; private set; } = null!;
    public IArcRepository ArcRepository { get; private set; } = null!;
    public IDialoguePlayer DialoguePlayer { get; private set; } = null!;
    public CallerGenerator CallerGenerator { get; private set; } = null!;
    public UIManager UIManager { get; private set; } = null!;
    public AsyncBroadcastLoop AsyncBroadcastLoop { get; private set; } = null!;
    public BroadcastCoordinator BroadcastCoordinator { get; private set; } = null!;
    public GlobalTransitionManager GlobalTransitionManager { get; private set; } = null!;
    public AdManager AdManager { get; private set; } = null!;
    public TranscriptRepository TranscriptRepository { get; private set; } = null!;
    public IScreeningController ScreeningController { get; private set; } = null!;

    // Provider interface implementations
    GameStateManager IProvide<GameStateManager>.Value() => GameStateManager;
    TimeManager IProvide<TimeManager>.Value() => TimeManager;
    EconomyManager IProvide<EconomyManager>.Value() => EconomyManager;
    ListenerManager IProvide<ListenerManager>.Value() => ListenerManager;
    SaveManager IProvide<SaveManager>.Value() => SaveManager;
    EventBus IProvide<EventBus>.Value() => EventBus;
    ICallerRepository IProvide<ICallerRepository>.Value() => CallerRepository;
    IArcRepository IProvide<IArcRepository>.Value() => ArcRepository;
    IDialoguePlayer IProvide<IDialoguePlayer>.Value() => DialoguePlayer;
    CallerGenerator IProvide<CallerGenerator>.Value() => CallerGenerator;
    UIManager IProvide<UIManager>.Value() => UIManager;
    AsyncBroadcastLoop IProvide<AsyncBroadcastLoop>.Value() => AsyncBroadcastLoop;
    BroadcastCoordinator IProvide<BroadcastCoordinator>.Value() => BroadcastCoordinator;
    GlobalTransitionManager IProvide<GlobalTransitionManager>.Value() => GlobalTransitionManager;
    AdManager IProvide<AdManager>.Value() => AdManager;
    ITranscriptRepository IProvide<ITranscriptRepository>.Value() => TranscriptRepository;
    IScreeningController IProvide<IScreeningController>.Value() => ScreeningController;
    IGameStateManager IProvide<IGameStateManager>.Value() => GameStateManager;
    ITimeManager IProvide<ITimeManager>.Value() => TimeManager;

    /// <summary>
    /// Initialize all service providers and register them with AutoInject.
    /// This should be called from _Ready() after all providers are created.
    /// Uses two-phase initialization: create all services first, then add to scene tree.
    /// </summary>
    public void Initialize()
    {
        GD.Print("ServiceProviderRoot: Starting two-phase service initialization...");

        // Phase 1: Create all service instances (without adding to scene tree)
        GD.Print("ServiceProviderRoot: Phase 1 - Creating all service instances...");

        // Create core services (plain classes) - no dependencies
        var eventBus = new EventBus();

        // Create arc repository first (needed by CallerRepository)
        var arcRepository = new ArcRepository();
        arcRepository.Initialize();

        // Create broadcast coordinator (needed by CallerRepository)
        var broadcastCoordinator = new BroadcastCoordinator();

        // Create caller repository with dependencies
        var callerRepo = new CallerRepository(arcRepository, broadcastCoordinator);

        // Create screening controller (depends on CallerRepository)
        var screeningController = new ScreeningController(callerRepo);

        // Resolve circular dependency
        callerRepo.ScreeningController = screeningController;

        // Create independent providers
        var saveManager = new SaveManager();
        var economyManager = new EconomyManager();

        // Create providers with dependencies
        var timeManager = new TimeManager();
        var gameStateManager = new GameStateManager();

        // Create listener manager with dependencies
        var listenerManager = new ListenerManager(gameStateManager, timeManager, callerRepo);

        // Create dialogue player
        var audioPlayer = new AudioDialoguePlayer(gameStateManager);

        // Create transcript repository
        var transcriptRepository = new TranscriptRepository();

        // Create caller generator
        var callerGenerator = new CallerGenerator(callerRepo, gameStateManager, arcRepository);

        // Create UI manager
        var uiManager = new UIManager();

        // Create broadcast services
        var asyncBroadcastLoop = new AsyncBroadcastLoop();

        // Create transition manager
        var globalTransitionManager = new GlobalTransitionManager();

        // Create ad manager
        var adManager = new AdManager();

        // Phase 2: Set all provider properties (now dependency injection will work)
        GD.Print("ServiceProviderRoot: Phase 2 - Setting provider properties...");

        EventBus = eventBus;
        ArcRepository = arcRepository;
        BroadcastCoordinator = broadcastCoordinator;
        CallerRepository = callerRepo;
        ScreeningController = screeningController;
        SaveManager = saveManager;
        EconomyManager = economyManager;
        TimeManager = timeManager;
        GameStateManager = gameStateManager;
        ListenerManager = listenerManager;
        DialoguePlayer = audioPlayer;
        TranscriptRepository = transcriptRepository;
        CallerGenerator = callerGenerator;
        UIManager = uiManager;
        AsyncBroadcastLoop = asyncBroadcastLoop;
        GlobalTransitionManager = globalTransitionManager;
        AdManager = adManager;

        // Make all services available BEFORE adding children to the scene tree
        GD.Print("ServiceProviderRoot: Making services available for dependency injection...");
        this.Provide();

        // Phase 3: Add Node-inheriting services to scene tree (triggers _Ready() and OnResolved())
        GD.Print("ServiceProviderRoot: Phase 3 - Adding Node services to scene tree...");

        // Only add services that inherit from Node
        AddChild(broadcastCoordinator);
        AddChild(saveManager);
        AddChild(economyManager);
        AddChild(timeManager);
        AddChild(gameStateManager);
        AddChild(listenerManager);
        AddChild(audioPlayer);
        AddChild(transcriptRepository);
        AddChild(callerGenerator);
        AddChild(uiManager);
        AddChild(asyncBroadcastLoop);
        AddChild(globalTransitionManager);
        AddChild(adManager);

        GD.Print("ServiceProviderRoot: All providers created and added to scene tree");
    }

    /// <summary>
    /// Called when node is ready and all dependencies are resolved.
    /// Provides all services to descendants in the scene tree.
    /// </summary>
    public void OnReady()
    {
        GD.Print("ServiceProviderRoot: Providing all services to descendants");
        
        // Initialize any providers that need initialization
        TimeManager.Initialize();
        GameStateManager.InitializeGame();
        EconomyManager.Initialize();
        ListenerManager.Initialize();
        SaveManager.Initialize();
        CallerGenerator.Initialize();
        
        // Services are already provided in Initialize() - don't call this.Provide() again
    }

    /// <summary>
    /// Called when all dependencies are resolved (required by IDependent).
    /// For the root provider, this just calls OnReady.
    /// </summary>
    public void OnResolved()
    {
        OnReady();
    }

    /// <summary>
    /// Called when the node is about to leave the scene tree.
    /// </summary>
    public void OnExitTree()
    {
        GD.Print("ServiceProviderRoot: Exiting scene tree");
    }
}