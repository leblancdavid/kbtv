# KBTV - Radio Talk Show Simulation

**KBTV** (Killer Bee Talk Show) is a radio talk show simulation game built in **Godot 4.5.1** using C#. Host your own late-night radio program, screen incoming callers, and build your audience through engaging conversations.

## 🎮 Game Overview

As Vern Tell, the enigmatic host of KBTV, you manage a live radio talk show where callers with paranormal experiences call in to share their stories. Your goal is to screen callers effectively, engage your audience, and grow your radio station's popularity.

### Key Features
- **Live Caller Screening**: Evaluate incoming callers in real-time
- **Dynamic Audience Response**: Listener count changes based on show quality
- **Real-time UI**: Live updates showing current callers, audience, and show status
- **Economic Management**: Earn money from shows and upgrade your equipment
- **Comprehensive Stats**: Track Vern's mood, energy, and performance metrics

## 🚀 Quick Start

### Requirements
- **Godot 4.5.1** or later
- **.NET 6.0** or later (for C# support)

### Setup
1. **Open in Godot**: Import the `kbtv_godot` directory
2. **Main Scene**: `scenes/Main.tscn` is automatically set as the main scene
3. **Run**: Press F5 or click Play

### Basic Gameplay
1. The game starts in Pre-Show phase
2. Use debug commands to start a live show
3. Callers will begin generating automatically
4. Screen callers using keyboard controls:
   - **Y**: Accept caller for show
   - **N**: Reject caller
   - **Space**: Put approved caller on air
   - **E**: End current call

## 🎯 Game Systems

### Core Systems
- **GameStateManager**: Controls show phases (PreShow → LiveShow → PostShow)
- **TimeManager**: Handles show timing and countdown
- **CallerQueue**: Manages caller lifecycle and waiting lists
- **CallerGenerator**: Creates diverse callers with different personalities
- **ListenerManager**: Tracks audience size and response to show events

### UI System
- **UIManagerBootstrap**: Main UI orchestrator
- **TabController**: Manages CALLERS/ITEMS/STATS tabs
- **InputHandler**: Processes player keyboard input
- **DebugHelper**: Testing and debugging tools

### Supporting Systems
- **EconomyManager**: Money tracking and transactions
- **SaveManager**: Persistence framework for game saves
- **VernStats**: Host character stats and mood system
- **Dialogue System**: Framework for conversation arcs (expandable)

## 📁 Project Structure

```
kbtv_godot/
├── scenes/
│   └── Main.tscn              # Main game scene
├── scripts/
│   ├── core/                  # Core game systems
│   │   ├── GameStateManager.cs
│   │   ├── GamePhase.cs
│   │   └── SingletonNode.cs
│   ├── managers/              # Game managers
│   │   ├── TimeManager.cs
│   │   └── ListenerManager.cs
│   ├── ui/                    # UI systems
│   │   ├── UIManagerBootstrap.cs
│   │   ├── InputHandler.cs
│   │   ├── DebugHelper.cs
│   │   ├── UIHelpers.cs
│   │   └── controllers/
│   │       ├── TabController.cs
│   │       └── TabDefinition.cs
│   ├── callers/               # Caller management
│   │   ├── Caller.cs
│   │   ├── CallerQueue.cs
│   │   ├── CallerGenerator.cs
│   │   └── Topic.cs
│   ├── data/                  # Data structures
│   │   ├── VernStats.cs
│   │   ├── Stat.cs
│   │   ├── VernMoodType.cs
│   │   ├── StatType.cs
│   │   └── StatModifier.cs
│   ├── economy/               # Money systems
│   │   ├── EconomyManager.cs
│   │   └── IncomeCalculator.cs
│   ├── dialogue/              # Conversation systems
│   │   ├── DialogueTypes.cs
│   │   ├── ConversationArc.cs
│   │   ├── ArcRepository.cs
│   │   ├── ArcJsonParser.cs
│   │   └── Templates/
│   │       └── VernDialogueTemplate.cs
│   ├── persistence/           # Save/load systems
│   │   ├── SaveManager.cs
│   │   ├── SaveData.cs
│   │   ├── ISaveable.cs
│   │   └── SerializableDictionary.cs
│   ├── upgrades/              # Equipment upgrades
│   │   ├── EquipmentConfig.cs
│   │   ├── EquipmentUpgrade.cs
│   │   └── EquipmentType.cs
│   └── ads/                   # Advertisement system
│       ├── AdData.cs
│       └── AdType.cs
├── TESTING_GUIDE.md           # Testing instructions
└── project.godot              # Godot configuration
```

## 🎮 Controls

### Keyboard Controls (During Live Show)
- **Y**: Accept/approve current screening caller
- **N**: Reject current screening caller
- **S**: Start screening next caller in queue
- **Space**: Put approved caller on air
- **E**: End current call

### Debug Controls
- **F12**: Show current game state in console

## 🛠️ Development

### Architecture Principles
- **SingletonNode Pattern**: All major systems inherit from `SingletonNode<T>` for global access
- **Event-Driven**: Systems communicate via C# events and Godot signals
- **Resource-Based**: Game data stored as Godot Resources (`.tres` files)
- **Modular UI**: Control-based UI system with reusable components

### Key Classes

#### GameStateManager
```csharp
public partial class GameStateManager : SingletonNode<GameStateManager>
{
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase, GamePhase> OnPhaseChanged;

    public void StartLiveShow() { /* ... */ }
    public void EndLiveShow() { /* ... */ }
}
```

#### UIManagerBootstrap
```csharp
public partial class UIManagerBootstrap : SingletonNode<UIManagerBootstrap>
{
    protected override void OnSingletonReady() {
        CreateCanvasUI();
        // Initialize tabs and UI components
    }
}
```

#### CallerQueue
```csharp
public partial class CallerQueue : SingletonNode<CallerQueue>
{
    public bool AddCaller(Caller caller) { /* ... */ }
    public Caller StartScreeningNext() { /* ... */ }
    public bool ApproveCurrentCaller() { /* ... */ }
    public Caller PutNextCallerOnAir() { /* ... */ }
}
```

### Adding New Features

1. **Create new system**: Inherit from `SingletonNode<T>` in appropriate directory
2. **Add to main scene**: Attach script to node in `scenes/Main.tscn`
3. **Initialize in `_Ready()`**: Set up references and event subscriptions
4. **Test integration**: Use `DebugHelper` for testing new functionality

## 🧪 Testing

### Debug Commands
Use the DebugHelper node methods in the Godot editor:

```csharp
// Start live show
GetNode("/root/Main/DebugHelper").StartShow();

// Spawn test caller
GetNode("/root/Main/DebugHelper").SpawnCaller();

// Check game state
GetNode("/root/Main/DebugHelper").ShowGameState();
```

### Automated Testing
- Run the game and verify all UI elements appear
- Test caller generation and screening workflow
- Verify audience response to show events
- Check save/load functionality

See `TESTING_GUIDE.md` for comprehensive testing procedures.

## 🔧 Configuration

### Input Actions
Defined in `project.godot`:
- `screen_accept`: Y key (accept caller)
- `screen_reject`: N key (reject caller)
- `start_screening`: S key (screen next caller)
- `put_on_air`: Space (put caller live)
- `end_call`: E key (end current call)

### Project Settings
- **Viewport**: 1920x1080
- **Rendering**: Forward Plus
- **C# Assembly**: KBTV
- **Main Scene**: scenes/Main.tscn

## 🚀 Building & Exporting

### For Development
1. Open in Godot 4.5.1+
2. Press F5 to run
3. Use built-in debugger and profiler

### For Distribution
1. **Project → Export**: Configure export presets
2. **Supported Platforms**: Windows, macOS, Linux
3. **Export Project**: Choose target platform

## 📚 Documentation

- **[TESTING_GUIDE.md](TESTING_GUIDE.md)**: Comprehensive testing procedures and debug commands
- **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)**: Complete API reference for all systems
- **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**: How to extend and modify the codebase
- **[CHANGELOG.md](CHANGELOG.md)**: Version history and migration details

## 📝 Technical Details

- **Engine**: Godot 4.5.1 with C# support
- **Architecture**: SingletonNode pattern with event-driven systems
- **UI**: Control-based responsive design
- **Data**: Resource-based storage (.tres files)
- **Events**: C# events with Godot signal integration
- **Persistence**: JSON-based save system
- **Input**: Keyboard controls with configurable actions

## 🔧 Development Requirements

- **Godot 4.5.1** or later
- **.NET 6.0** or later
- **C# development environment**

## 🤝 Contributing

1. **Read** the developer guide (`DEVELOPER_GUIDE.md`)
2. **Test** thoroughly using `TESTING_GUIDE.md`
3. **Follow** the established patterns and architecture
4. **Document** any new features or changes
5. **Submit** a pull request with clear description

## 📄 License

This project is open source. See repository root for license details.

## 🎯 Project Status

**✅ COMPLETE**: Full-featured radio talk show simulation game
- Core gameplay loop functional
- All major systems implemented
- Comprehensive testing tools
- Ready for extension and deployment

---

*Built with ❤️ using Godot 4.5.1*