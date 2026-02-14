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
using KBTV.Audio;
using KBTV.Monitors;
using KBTV.Broadcast;
using KBTV.Items;

namespace KBTV.Core
{

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
    IProvide<CallerGenerator>,
    IProvide<UIManager>,
    IProvide<IUIManager>,
    IProvide<AsyncBroadcastLoop>,
    IProvide<BroadcastTimer>,
    IProvide<BroadcastStateManager>,
    IProvide<GlobalTransitionManager>,
    IProvide<AdManager>,
    IProvide<ITranscriptRepository>,
    IProvide<IScreeningController>,
    IProvide<IGameStateManager>,
    IProvide<ITimeManager>,
    IProvide<IBroadcastAudioService>,
    IProvide<IUIAudioService>,
    IProvide<DeadAirManager>,
    IProvide<ConversationStatTracker>,
    IProvide<TopicManager>,
    IProvide<ItemManager>,
    IProvide<ModalManager>,
    IProvide<IEvidenceAnalyzer>,
    IProvide<IEvidenceCabinet>,
    IProvide<IEvidenceWebsite>
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
    public CallerGenerator CallerGenerator { get; private set; } = null!;
    public UIManager UIManager { get; private set; } = null!;
    public AsyncBroadcastLoop AsyncBroadcastLoop { get; private set; } = null!;
    public BroadcastTimer BroadcastTimer { get; private set; } = null!;
    public BroadcastStateManager BroadcastStateManager { get; private set; } = null!;
    public GlobalTransitionManager GlobalTransitionManager { get; private set; } = null!;
    public AdManager AdManager { get; private set; } = null!;
    public TranscriptRepository TranscriptRepository { get; private set; } = null!;
    public IScreeningController ScreeningController { get; private set; } = null!;
    public IBroadcastAudioService BroadcastAudioService { get; private set; } = null!;
    public IUIAudioService UIAudioService { get; private set; } = null!;
    public DeadAirManager DeadAirManager { get; private set; } = null!;
    public ConversationStatTracker ConversationStatTracker { get; private set; } = null!;
    public TopicManager TopicManager { get; private set; } = null!;
    public ItemManager ItemManager { get; private set; } = null!;
    public ModalManager ModalManager { get; private set; } = null!;
    public EvidenceAnalyzer EvidenceAnalyzer { get; private set; } = null!;
    public EvidenceCabinet EvidenceCabinet { get; private set; } = null!;
    public EvidenceWebsite EvidenceWebsite { get; private set; } = null!;

    // Provider interface implementations
    GameStateManager IProvide<GameStateManager>.Value() => GameStateManager;
    TimeManager IProvide<TimeManager>.Value() => TimeManager;
    EconomyManager IProvide<EconomyManager>.Value() => EconomyManager;
    ListenerManager IProvide<ListenerManager>.Value() => ListenerManager;
    SaveManager IProvide<SaveManager>.Value() => SaveManager;
    EventBus IProvide<EventBus>.Value() => EventBus;
    ICallerRepository IProvide<ICallerRepository>.Value() => CallerRepository;
    IArcRepository IProvide<IArcRepository>.Value() => ArcRepository;
    CallerGenerator IProvide<CallerGenerator>.Value() => CallerGenerator;
    UIManager IProvide<UIManager>.Value() => UIManager;
    IUIManager IProvide<IUIManager>.Value() => UIManager;
    AsyncBroadcastLoop IProvide<AsyncBroadcastLoop>.Value() => AsyncBroadcastLoop;
    BroadcastTimer IProvide<BroadcastTimer>.Value() => BroadcastTimer;
    BroadcastStateManager IProvide<BroadcastStateManager>.Value() => BroadcastStateManager;
    GlobalTransitionManager IProvide<GlobalTransitionManager>.Value() => GlobalTransitionManager;
    AdManager IProvide<AdManager>.Value() => AdManager;
    ITranscriptRepository IProvide<ITranscriptRepository>.Value() => TranscriptRepository;
    IScreeningController IProvide<IScreeningController>.Value() => ScreeningController;
    IGameStateManager IProvide<IGameStateManager>.Value() => GameStateManager;
    ITimeManager IProvide<ITimeManager>.Value() => TimeManager;
    IBroadcastAudioService IProvide<IBroadcastAudioService>.Value() => BroadcastAudioService;
    IUIAudioService IProvide<IUIAudioService>.Value() => UIAudioService;
    DeadAirManager IProvide<DeadAirManager>.Value() => DeadAirManager;
    ConversationStatTracker IProvide<ConversationStatTracker>.Value() => ConversationStatTracker;
    TopicManager IProvide<TopicManager>.Value() => TopicManager;
    ItemManager IProvide<ItemManager>.Value() => ItemManager;
    ModalManager IProvide<ModalManager>.Value() => ModalManager;
    IEvidenceAnalyzer IProvide<IEvidenceAnalyzer>.Value() => EvidenceAnalyzer;
    IEvidenceCabinet IProvide<IEvidenceCabinet>.Value() => EvidenceCabinet;
    IEvidenceWebsite IProvide<IEvidenceWebsite>.Value() => EvidenceWebsite;

    /// <summary>
    /// Initialize all service providers and register them with AutoInject.
    /// This should be called from _Ready() after all providers are created.
    /// Uses two-phase initialization: create all services first, then add to scene tree.
    /// </summary>
    public void Initialize()
    {
        Log.Debug("ServiceProviderRoot: Starting two-phase service initialization...");
        InitializeServices();
    }

    private void InitializeServices()
    {
        // Phase 1: Create all services in dependency order
        Log.Debug("ServiceProviderRoot: Phase 1 - Creating services...");

        // Create event bus first (no dependencies)
        var eventBus = new EventBus();

        // Create arc repository (no dependencies)
        var arcRepository = new ArcRepository();
        arcRepository.Initialize();

        // Create caller repository (depends on ArcRepository)
        var callerRepo = new CallerRepository(arcRepository);

        // Create topic manager
        var topicManager = new TopicManager();

        // Create save manager early (needed by ScreeningController)
        var saveManager = new SaveManager();

        // Create providers with dependencies
        var timeManager = new TimeManager();
        var gameStateManager = new GameStateManager();
        gameStateManager.InitializeGame();

        // Create listener manager with dependencies
        var listenerManager = new ListenerManager(gameStateManager, timeManager, callerRepo);

        // Create transcript repository
        var transcriptRepository = new TranscriptRepository();

        // Create caller generator
        var callerGenerator = new CallerGenerator(callerRepo, gameStateManager, arcRepository);

        // Create screening controller (now has GameStateManager dependency)
        var screeningController = new ScreeningController(callerRepo, topicManager, saveManager, gameStateManager);

        // Resolve circular dependency
        callerRepo.ScreeningController = screeningController;

        // Create independent providers
        var economyManager = new EconomyManager();
        var itemManager = new ItemManager();
        var modalManager = new ModalManager();

        // Create UI manager
        var uiManager = new UIManager();

        // Create broadcast services
        var asyncBroadcastLoop = new AsyncBroadcastLoop();
        var broadcastTimer = new BroadcastTimer();
        var broadcastStateManager = new BroadcastStateManager();
        var globalTransitionManager = new GlobalTransitionManager();
        var adManager = new AdManager();
        var broadcastAudioService = new BroadcastAudioService();
        var uiAudioService = new UIAudioService();
        var deadAirManager = new DeadAirManager();

        // Create conversation stat tracker (depends on GameStateManager.VernStats)
        var conversationStatTracker = new ConversationStatTracker(gameStateManager, topicManager);

        // Create evidence system services
        var evidenceAnalyzer = new EvidenceAnalyzer();
        var evidenceCabinet = new EvidenceCabinet();
        var evidenceWebsite = new EvidenceWebsite();
        Log.Debug($"ServiceProviderRoot: Created evidence services - Analyzer: {evidenceAnalyzer != null}");

        // Phase 2: Set all provider properties (now dependency injection will work)
        Log.Debug("ServiceProviderRoot: Phase 2 - Setting provider properties...");

        EventBus = eventBus;
        ArcRepository = arcRepository;
        CallerRepository = callerRepo;
        ScreeningController = screeningController;
        SaveManager = saveManager;
        EconomyManager = economyManager;
        TimeManager = timeManager;
        GameStateManager = gameStateManager;
        ListenerManager = listenerManager;
        TranscriptRepository = transcriptRepository;
        CallerGenerator = callerGenerator;
        UIManager = uiManager;
        AsyncBroadcastLoop = asyncBroadcastLoop;
        BroadcastTimer = broadcastTimer;
        BroadcastStateManager = broadcastStateManager;
        GlobalTransitionManager = globalTransitionManager;
        AdManager = adManager;
        BroadcastAudioService = broadcastAudioService;
        UIAudioService = uiAudioService;
        DeadAirManager = deadAirManager;
        ConversationStatTracker = conversationStatTracker;
        TopicManager = topicManager;
        ItemManager = itemManager;
        ModalManager = modalManager;
        EvidenceAnalyzer = evidenceAnalyzer;
        EvidenceCabinet = evidenceCabinet;
        EvidenceWebsite = evidenceWebsite;

        // Make all services available BEFORE adding children to the scene tree
        Log.Debug("ServiceProviderRoot: Making services available for dependency injection...");
        this.Provide();

        // Phase 3: Add Node-inheriting services to scene tree (triggers _Ready() and OnResolved())
        Log.Debug("ServiceProviderRoot: Phase 3 - Adding Node services to scene tree...");

        // Only add services that inherit from Node
        AddChild(saveManager);
        AddChild(economyManager);
        AddChild(timeManager);
        AddChild(gameStateManager);
        AddChild(listenerManager);
        AddChild(transcriptRepository);
        AddChild(callerGenerator);
        AddChild(uiManager);
        AddChild(asyncBroadcastLoop);
        AddChild(broadcastTimer);
        AddChild(broadcastStateManager);
        AddChild(globalTransitionManager);
        AddChild(adManager);
        AddChild(broadcastAudioService);
        AddChild(uiAudioService);
        AddChild(deadAirManager);
        AddChild(itemManager);
        AddChild(modalManager);
        AddChild(evidenceAnalyzer);
        AddChild(evidenceCabinet);
        AddChild(evidenceWebsite);

        Log.Debug("ServiceProviderRoot: All providers created and added to scene tree");
    }

    /// <summary>
    /// Called when node is ready and all dependencies are resolved.
    /// Provides all services to descendants in the scene tree.
    /// </summary>
    public void OnReady()
    {
        Log.Debug("ServiceProviderRoot: Providing all services to descendants");
        
        // Initialize any providers that need initialization
        TimeManager.Initialize();
        EconomyManager.Initialize();
        ListenerManager.Initialize();
        SaveManager.Initialize();
        SaveManager.Load();
        CallerGenerator.Initialize();
        Log.Debug("ServiceProviderRoot: Calling EvidenceAnalyzer.Initialize()");
        EvidenceAnalyzer.Initialize();
        Log.Debug($"ServiceProviderRoot: After Initialize - Evidence count: {EvidenceAnalyzer.GetIdentifiedCount()} identified");
        EvidenceCabinet.Initialize();
        EvidenceWebsite.Initialize();

        // Register saveables with SaveManager
        SaveManager.RegisterSaveable(TopicManager);

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
        Log.Debug("ServiceProviderRoot: Exiting scene tree");

        if (EventBus != null)
        {
            EventBus.Clear();
            Log.Debug("ServiceProviderRoot: Cleared EventBus subscribers");
        }
    }
    }
}