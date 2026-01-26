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
    /// </summary>
    public void Initialize()
    {
        // Create core services (plain classes) - no dependencies
        GD.Print("ServiceProviderRoot: Creating core services...");
        
        EventBus = new EventBus();
        
        // Create arc repository first (needed by CallerRepository)
        var arcRepository = new ArcRepository();
        arcRepository.Initialize();
        ArcRepository = arcRepository;

        // Create broadcast coordinator (needed by CallerRepository)
        BroadcastCoordinator = new BroadcastCoordinator();
        AddChild(BroadcastCoordinator);
        
        var callerRepo = new CallerRepository(ArcRepository, BroadcastCoordinator);
        CallerRepository = callerRepo;
        
        // Create screening controller (depends on CallerRepository)
        var screeningController = new ScreeningController(callerRepo);
        ScreeningController = screeningController;
        
        // Set circular dependency property
        callerRepo.ScreeningController = screeningController;
        
        // Create independent providers
        SaveManager = new SaveManager();
        AddChild(SaveManager);
        
        EconomyManager = new EconomyManager();
        AddChild(EconomyManager);

        // Create providers with dependencies
        TimeManager = new TimeManager();
        AddChild(TimeManager);
        
        GameStateManager = new GameStateManager();
        AddChild(GameStateManager);
        
        ListenerManager = new ListenerManager(GameStateManager, TimeManager, CallerRepository);
        AddChild(ListenerManager);

        // Create dialogue player
        var audioPlayer = new AudioDialoguePlayer(GameStateManager);
        AddChild(audioPlayer);
        DialoguePlayer = audioPlayer;

        // Create transcript repository
        TranscriptRepository = new TranscriptRepository();
        AddChild(TranscriptRepository);

        // Create caller generator
        CallerGenerator = new CallerGenerator(CallerRepository, GameStateManager, ArcRepository);
        AddChild(CallerGenerator);

        // Create UI manager
        UIManager = new UIManager();
        AddChild(UIManager);

        // Create broadcast services
        AsyncBroadcastLoop = new AsyncBroadcastLoop();
        AddChild(AsyncBroadcastLoop);

        // Create transition manager
        GlobalTransitionManager = new GlobalTransitionManager();
        AddChild(GlobalTransitionManager);

        // Create ad manager
        AdManager = new AdManager();
        AddChild(AdManager);

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
        
        // Make all services available to descendants
        this.Provide();
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