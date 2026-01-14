# KBTV Game Systems API Documentation

This document provides detailed API documentation for all major systems in the KBTV Godot project.

## Table of Contents
- [Core Systems](#core-systems)
- [Manager Systems](#manager-systems)
- [UI Systems](#ui-systems)
- [Caller Systems](#caller-systems)
- [Data Systems](#data-systems)

---

## Core Systems

### GameStateManager

**Location**: `scripts/core/GameStateManager.cs`  
**Purpose**: Manages overall game state and phase transitions

#### Properties
```csharp
public GamePhase CurrentPhase { get; private set; }
public int CurrentNight { get; private set; }
public VernStats VernStats { get; private set; }
public bool IsLive => CurrentPhase == GamePhase.LiveShow;
```

#### Events
```csharp
public event Action<GamePhase, GamePhase> OnPhaseChanged;
```

#### Methods
```csharp
public void StartLiveShow()
public void EndLiveShow()
public void AdvanceNight()
```

#### Usage
```csharp
var gameState = GameStateManager.Instance;
gameState.OnPhaseChanged += HandlePhaseChange;
if (gameState.IsLive) { /* Live show logic */ }
```

---

### SingletonNode<T>

**Location**: `scripts/core/SingletonNode.cs`  
**Purpose**: Base class for singleton game systems

#### Methods to Override
```csharp
protected virtual void OnSingletonReady() { }
// Called when singleton is ready (use instead of Awake)

public override void _Ready() { }
// Called by Godot (don't override unless necessary)
```

#### Usage
```csharp
public partial class MyManager : SingletonNode<MyManager>
{
    protected override void OnSingletonReady()
    {
        // Initialize your manager here
    }
}
```

---

## Manager Systems

### TimeManager

**Location**: `scripts/managers/TimeManager.cs`  
**Purpose**: Handles game time, show countdown, and tick events

#### Properties
```csharp
public float ShowDuration { get; private set; } = 300f; // 5 minutes
public float ElapsedTime { get; private set; }
public float RemainingShowTime => ShowDuration - ElapsedTime;
public bool IsShowActive { get; private set; }
```

#### Events
```csharp
public event Action<float> OnTick; // deltaTime parameter
public event Action OnShowStarted;
public event Action OnShowEnded;
```

#### Methods
```csharp
public void StartShow()
public void EndShow()
public void ResetTimer()
```

---

### ListenerManager

**Location**: `scripts/managers/ListenerManager.cs`  
**Purpose**: Tracks audience size and response to show events

#### Properties
```csharp
public int CurrentListeners { get; private set; }
public int PeakListeners { get; private set; }
public int StartingListeners { get; private set; }
public int ListenerChange => CurrentListeners - StartingListeners;
public string GetFormattedListeners()
public string GetFormattedChange()
```

#### Events
```csharp
public event Action<int, int> OnListenersChanged; // (oldCount, newCount)
public event Action<int> OnPeakReached; // (peakCount)
```

#### Methods
```csharp
public void ModifyListeners(int amount)
public void InitializeListeners() // Called at show start
```

---

## UI Systems

### UIManagerBootstrap

**Location**: `scripts/ui/UIManagerBootstrap.cs`  
**Purpose**: Main UI orchestrator and scene setup

#### Properties
```csharp
public VernStats GetVernStats()
```

#### UI Components
```csharp
// Header elements
private Label _clockText;
private Label _remainingText;
private Label _listenerCount;
private Label _listenerChange;
private Control _liveIndicator;

// Footer elements
private Label _moneyText;
```

#### Methods
```csharp
private void CreateCanvasUI() // Sets up main UI canvas
private void CreateHeaderBar(Control parent)
private void CreateFooter(Control parent)
private void PopulateCallersContent(Control contentArea)
private void PopulateItemsContent(Control contentArea)
private void PopulateStatsContent(Control contentArea)
```

#### Tab System
```csharp
private TabController _tabController;
private void InitializeTabs() // CALLERS, ITEMS, STATS
```

---

### TabController

**Location**: `scripts/ui/controllers/TabController.cs`  
**Purpose**: Manages tabbed interface with scrollable content

#### Properties
```csharp
private int _currentTab;
private List<TabDefinition> _tabs;
```

#### Methods
```csharp
public void Initialize(Control parent)
public void SwitchTab(int tabIndex)
public void RefreshTabContent(int tabIndex)
public void RefreshCurrentTab()
public void RefreshAllTabs()
```

#### TabDefinition Structure
```csharp
public partial class TabDefinition
{
    public string Name { get; set; }
    public Action<Control> PopulateContent { get; set; }
    public Action OnTabSelected { get; set; }
}
```

---

### InputHandler

**Location**: `scripts/ui/InputHandler.cs`  
**Purpose**: Processes player input during gameplay

#### Input Actions (defined in project.godot)
```csharp
screen_accept: Key.Y      // Accept caller
screen_reject: Key.N      // Reject caller
end_call: Key.E          // End current call
start_screening: Key.S   // Screen next caller
put_on_air: Key.Space    // Put caller on air
```

#### Methods
```csharp
public override void _Input(InputEvent @event)
private void HandleKeyInput(Key keycode)
```

---

### DebugHelper

**Location**: `scripts/ui/DebugHelper.cs`  
**Purpose**: Testing and debugging utilities

#### Debug Methods
```csharp
public void StartShow()
public void SpawnCaller()
public void ApproveCaller()
public void RejectCaller()
public void EndCall()
public void PutNextOnAir()
public void ShowGameState() // Press F12 in-game
```

---

## Caller Systems

### CallerQueue

**Location**: `scripts/callers/CallerQueue.cs`  
**Purpose**: Manages caller lifecycle and queue operations

#### Properties
```csharp
public Godot.Collections.Array<Caller> IncomingCallers { get; }
public Godot.Collections.Array<Caller> OnHoldCallers { get; }
public Caller CurrentScreening { get; }
public Caller OnAirCaller { get; }
public int TotalWaiting => IncomingCallers.Count + OnHoldCallers.Count;
public bool HasIncomingCallers => IncomingCallers.Count > 0;
public bool IsScreening => CurrentScreening != null;
public bool CanAcceptMoreCallers => IncomingCallers.Count < _maxQueueSize;
```

#### Events
```csharp
public event Action<Caller> OnCallerAdded;
public event Action<Caller> OnCallerRemoved;
public event Action<Caller> OnCallerOnAir;
public event Action<Caller> OnCallerCompleted;
public event Action<Caller> OnCallerApproved;
public event Action<Caller> OnCallerDisconnected;
```

#### Methods
```csharp
public bool AddCaller(Caller caller)
public Caller StartScreeningNext()
public bool ApproveCurrentCaller()
public bool RejectCurrentCaller()
public Caller PutNextCallerOnAir()
public Caller EndCurrentCall()
public void ClearAll()
```

---

### CallerGenerator

**Location**: `scripts/callers/CallerGenerator.cs`  
**Purpose**: Generates diverse callers during live shows

#### Configuration Properties
```csharp
[Export] private float _minSpawnInterval = 1f;
[Export] private float _maxSpawnInterval = 3f;
[Export] private float _basePatience = 30f;
[Export] private float _fakeCallerChance = 0.15f;
[Export] private float _compellingCallerChance = 0.1f;
```

#### Methods
```csharp
public void StartGenerating()
public void StopGenerating()
public Caller SpawnCaller() // Manual spawn for testing
private Caller GenerateRandomCaller()
```

#### Caller Generation Data
```csharp
private static readonly string[] FirstNames = { "John", "Mike", "Dave", ... };
private static readonly string[] LastNames = { "Smith", "Johnson", ... };
private static readonly string[] Locations = { "Springfield", "Riverside", ... };
private static readonly string[] Topics = { "Ghosts", "UFOs", "Bigfoot", ... };
```

---

### Caller

**Location**: `scripts/callers/Caller.cs`  
**Purpose**: Represents an individual caller with all their properties

#### Identity Properties
```csharp
public string Name { get; }
public string PhoneNumber { get; }
public string Location { get; }
public string ClaimedTopic { get; }
public string ActualTopic { get; }
public string CallReason { get; }
```

#### Attributes
```csharp
public CallerLegitimacy Legitimacy { get; }
public CallerPhoneQuality PhoneQuality { get; }
public CallerEmotionalState EmotionalState { get; }
public CallerCurseRisk CurseRisk { get; }
public CallerBeliefLevel BeliefLevel { get; }
public CallerEvidenceLevel EvidenceLevel { get; }
public CallerCoherence Coherence { get; }
public CallerUrgency Urgency { get; }
public CallerState State { get; }
public float Patience { get; }
public float Quality { get; }
```

#### State Methods
```csharp
public void SetState(CallerState newState)
public bool UpdateWaitTime(float deltaTime)
public void ResetRevelations()
public void UpdateRevelations(float deltaTime)
public float CalculateShowImpact(string currentTopic)
```

#### Revelation System
```csharp
public PropertyRevelation[] Revelations { get; }
public PropertyRevelation GetRevelation(string propertyName)
public PropertyRevelation GetNextRevelation()
public Godot.Collections.Array<PropertyRevelation> GetRevealedProperties()
```

---

## Data Systems

### VernStats

**Location**: `scripts/data/VernStats.cs`  
**Purpose**: Manages host character stats and mood system

#### Stat Properties
```csharp
// Dependencies
public Stat Caffeine { get; }
public Stat Nicotine { get; }

// Physical
public Stat Energy { get; }
public Stat Satiety { get; }

// Emotional
public Stat Spirit { get; }

// Cognitive
public Stat Alertness { get; }
public Stat Discernment { get; }
public Stat Focus { get; }
public Stat Patience { get; }

// Long-term
public Stat Skepticism { get; }
```

#### Key Methods
```csharp
public void ApplyDecay(float deltaTime, float decayMultiplier = 1f, float stressLevel = 0f)
public VernMoodType CalculateMoodType()
public float CalculateVIBE()
public void ModifyStat(StatType type, float amount)
```

#### Stat Modification
```csharp
public void ApplyGoodCallerEffect()
public void ApplyGreatCallerEffect()
public void ApplyBadCallerEffect()
public void ApplyBeliefChange(float amount)
```

---

### Stat

**Location**: `scripts/data/Stat.cs`  
**Purpose**: Individual stat with min/max bounds and modification

#### Properties
```csharp
public float Value { get; }
public float Normalized => Mathf.Clamp01((Value - Min) / (Max - Min));
public bool IsAtMin => Mathf.Approximately(Value, Min);
public bool IsAtMax => Mathf.Approximately(Value, Max);
```

#### Methods
```csharp
public void SetValue(float value)
public void Modify(float amount)
public void SetMinMax(float min, float max)
```

---

### StatModifier

**Location**: `scripts/data/StatModifier.cs`  
**Purpose**: Defines stat modifications from items/events

#### Properties
```csharp
public string DisplayName { get; }
public string Description { get; }
public StatModification[] Modifications { get; }
```

#### Methods
```csharp
public void Apply(VernStats stats)
```

---

### StatModification

**Location**: `scripts/data/StatModifier.cs`  
**Purpose**: Single stat modification entry

```csharp
public struct StatModification
{
    public StatType StatType;
    public float Amount;
}
```

---

## Supporting Systems

### EconomyManager

**Location**: `scripts/economy/EconomyManager.cs`  
**Purpose**: Handles player money and transactions

#### Properties
```csharp
public int CurrentMoney { get; }
```

#### Events
```csharp
public event Action<int, int> OnMoneyChanged; // (oldAmount, newAmount)
public event Action<int, string> OnPurchase; // (amount, reason)
public event Action<int> OnPurchaseFailed; // (amount)
```

#### Methods
```csharp
public bool CanAfford(int amount)
public void AddMoney(int amount, string reason = null)
public bool SpendMoney(int amount, string reason = null)
public void SetMoney(int amount)
```

---

### SaveManager

**Location**: `scripts/persistence/SaveManager.cs`  
**Purpose**: Handles game save/load functionality

#### Properties
```csharp
public SaveData CurrentSave { get; }
public bool IsDirty { get; }
public bool HasSave { get; }
```

#### Events
```csharp
public event Action OnSaveCompleted;
public event Action OnLoadCompleted;
public event Action OnSaveDeleted;
public event Action OnDataChanged;
```

#### Methods
```csharp
public void Load()
public void Save()
public void DeleteSave()
public void MarkDirty()
public SaveData CreateNewSave()
```

---

### ISaveable

**Location**: `scripts/persistence/ISaveable.cs`  
**Purpose**: Interface for components that save/load data

```csharp
public interface ISaveable
{
    void OnBeforeSave(SaveData data);
    void OnAfterLoad(SaveData data);
}
```

---

## Enums and Constants

### GamePhase
```csharp
public enum GamePhase
{
    PreShow,
    LiveShow,
    PostShow
}
```

### VernMoodType
```csharp
public enum VernMoodType
{
    Neutral,
    Tired,
    Energized,
    Irritated,
    Gruff,
    Amused,
    Focused
}
```

### StatType
```csharp
public enum StatType
{
    // Dependencies
    Caffeine, Nicotine,
    // Physical
    Energy, Satiety,
    // Emotional
    Spirit,
    // Cognitive
    Alertness, Discernment, Focus, Patience,
    // Long-term
    Skepticism
}
```

### CallerState
```csharp
public enum CallerState
{
    Incoming,
    Screening,
    OnHold,
    OnAir,
    Completed,
    Rejected,
    Disconnected
}
```

### CallerLegitimacy
```csharp
public enum CallerLegitimacy
{
    Fake,
    Questionable,
    Credible,
    Compelling
}
```

---

## Usage Examples

### Starting a Live Show
```csharp
// Get managers
var gameState = GameStateManager.Instance;
var callerGen = CallerGenerator.Instance;

// Start the show
gameState.StartLiveShow();
callerGen.StartGenerating();

// Listen for phase changes
gameState.OnPhaseChanged += (oldPhase, newPhase) => {
    GD.Print($"Phase changed: {oldPhase} -> {newPhase}");
};
```

### Screening Callers
```csharp
var callerQueue = CallerQueue.Instance;

// Start screening a caller
var caller = callerQueue.StartScreeningNext();
if (caller != null) {
    GD.Print($"Now screening: {caller.Name}");
}

// Approve or reject
if (callerQueue.ApproveCurrentCaller()) {
    GD.Print("Caller approved!");
}

// Put on air
var onAir = callerQueue.PutNextCallerOnAir();
if (onAir != null) {
    GD.Print($"{onAir.Name} is now on air!");
}
```

### Managing Vern's Stats
```csharp
var vernStats = VernStats.Instance;

// Apply stat decay each frame
vernStats.ApplyDecay(deltaTime);

// Check current VIBE score
float vibe = vernStats.CalculateVIBE();
VernMoodType mood = vernStats.CalculateMoodType();

// Apply effects
vernStats.ApplyGoodCallerEffect();
```

### UI Updates
```csharp
var uiManager = UIManagerBootstrap.Instance;

// Refresh current tab
uiManager._tabController.RefreshCurrentTab();

// Get formatted display values
string listeners = ListenerManager.Instance.GetFormattedListeners();
string money = $"${EconomyManager.Instance.CurrentMoney}";
```