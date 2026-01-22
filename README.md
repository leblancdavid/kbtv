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
1. **Open in Godot**: Import the project by selecting the `project.godot` file
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
kbtv/
├── scenes/
│   └── Main.tscn              # Main game scene with all managers
├── scripts/
│   ├── core/                  # Core patterns and systems
│   │   ├── ServiceRegistry.cs # Service registry for dependency injection
│   │   ├── GameStateManager.cs
│   │   ├── GamePhase.cs
│   │   ├── EventAggregator.cs
│   │   └── patterns/
│   │       └── Result.cs      # Result<T> type for error handling
│   ├── managers/              # Game managers
│   │   ├── TimeManager.cs
│   │   └── ListenerManager.cs
│   ├── ui/                    # UI systems
│   │   ├── UIManager.cs
│   │   ├── InputHandler.cs
│   │   ├── DebugHelper.cs
│   │   ├── UIHelpers.cs
│   │   ├── themes/
│   │   │   └── UIColors.cs
│   │   ├── components/
│   │   │   └── ReactiveListPanel.cs
│   │   └── controllers/
│   │       └── TabDefinition.cs
│   ├── callers/               # Caller management
│   │   ├── Caller.cs
│   │   ├── CallerQueue.cs
│   │   ├── CallerGenerator.cs
│   │   ├── CallerRepository.cs
│   │   └── Topic.cs
│   ├── screening/             # Screening workflow
│   │   ├── ScreeningController.cs
│   │   └── ScreeningSession.cs
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
├── docs/                      # Documentation
│   ├── technical/
│   │   └── TECHNICAL_SPEC.md
│   ├── ui/
│   │   └── UI_IMPLEMENTATION.md
│   └── testing/
│       └── TESTING.md
├── tests/                     # Test files
│   ├── unit/
│   └── integration/
└── project.godot              # Godot configuration
├── assets/
│   └── audio/
│       ├── voice/
│       │   ├── Vern/           # Vern Tell voice lines (AI-generated)
│       │   └── Callers/        # Caller dialogue audio (AI-generated)
│       └── bumpers/            # Show transition audio
└── Tools/
    └── AudioGeneration/        # Voice cloning and audio generation scripts
```

## 🎵 Audio Generation System

KBTV uses AI-powered voice synthesis to generate realistic dialogue audio for Vern Tell (host) and all callers.

### Prerequisites

- **ElevenLabs API Account**: Sign up at [elevenlabs.io](https://elevenlabs.io)
- **API Key**: Get your API key from the ElevenLabs dashboard
- **Python 3.8+**: Required for audio generation scripts

### Setup

1. **Install Dependencies**:
   ```bash
   cd Tools/AudioGeneration
   pip install elevenlabs requests
   ```

2. **Configure API Key**:
   Create `elevenlabs_config.json`:
   ```json
   {
     "elevenlabs_api_key": "your_api_key_here"
   }
   ```
   Or set environment variable: `export ELEVENLABS_API_KEY=your_key`

3. **Voice Cloning** (Vern Tell):
   ```bash
   cd Tools/AudioGeneration
   python voice_setup.py  # Upload reference audio and create Vern voice
   ```

### Audio Generation Commands

#### Vern Broadcast Audio
Generate Vern audio for show openings, closings, between-callers, etc.:

```bash
cd Tools/AudioGeneration
python generate_vern_broadcast.py              # Skip existing files
python generate_vern_broadcast.py --force      # Regenerate all files
python generate_vern_broadcast.py --verbose    # Detailed progress output
```

#### Vern Conversation Audio
Generate Vern dialogue for conversation arcs:

```bash
cd Tools/AudioGeneration
python generate_vern_audio.py              # Skip existing files
python generate_vern_audio.py --force      # Regenerate all files
python generate_vern_audio.py --verbose    # Detailed progress output
```

#### Caller Dialogue Audio
Generate caller voice lines for conversation arcs:

```bash
cd Tools/AudioGeneration
python generate_caller_audio.py              # Skip existing files
python generate_caller_audio.py --force      # Regenerate all files
python generate_caller_audio.py --verbose    # Detailed progress output
python generate_caller_audio.py --arc ufos_compelling_pilot  # Specific arc only
python generate_caller_audio.py --arc ghosts --force        # Regenerate topic
```

#### Intelligent Caller Generation
Generate caller audio with personality-based voice selection:

```bash
cd Tools/AudioGeneration
python generate_intelligent_caller.py --arc ufos_compelling_pilot
```

### Voice System Architecture

#### Voice Archetypes
Callers use different voice archetypes based on personality and topic:

| Archetype | Description | Use Case |
|-----------|-------------|----------|
| `default_male/female` | Neutral, professional voices | Credible witnesses |
| `enthusiastic` | Excited, animated delivery | Compelling stories |
| `nervous` | Hesitant, shaky speech | Questionable claims |
| `gruff` | Rough, experienced tone | Cryptid hunters |
| `conspiracy` | Intense, conspiratorial | Conspiracy theorists |
| `elderly_male/female` | Aged, wise voices | Veteran callers |

#### File Organization
```
assets/audio/voice/
├── Vern/
│   ├── ConversationArcs/     # Vern responses in conversations
│   ├── MainBroadcast/        # Show openings/closings
│   └── Transitions/          # Between-caller banter
└── Callers/
    ├── UFOs/                 # UFO-related caller audio
    ├── Ghosts/               # Ghost story caller audio
    ├── Cryptids/             # Cryptid caller audio
    └── Conspiracies/         # Conspiracy caller audio
```

#### Naming Convention
- **Vern**: `{mood}_{line_type}_{index}.mp3`
- **Callers**: `{arc_id}_{gender}_{line_index}.mp3`

### Cost Optimization

ElevenLabs charges per character of generated audio. The system includes smart features to minimize costs:

- **Smart Skipping**: `--force` flag to regenerate only changed content
- **Batch Processing**: Generate by topic/arc to avoid API limits
- **File Caching**: Skip regeneration of existing valid files
- **Rate Limiting**: 0.5 second delays between API calls

### Troubleshooting

**API Key Issues**:
```bash
# Check API key configuration
python -c "from elevenlabs_setup import ElevenLabsVoiceCloner; print('API configured:', ElevenLabsVoiceCloner().api_key is not None)"
```

**Voice Cloning Failed**:
- Ensure reference audio is high quality (44.1kHz WAV, clean recording)
- Check ElevenLabs account has voice cloning credits

**Files Not Generated**:
- Check write permissions in `assets/audio/` directory
- Verify API key has sufficient credits
- Use `--verbose` flag for detailed error messages

**Godot Integration**:
- Audio files load via `res://assets/audio/voice/...` paths
- Ensure files are committed to version control
- Check Godot import settings for audio files

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
- **Service Registry Pattern**: All major systems are registered in `ServiceRegistry` for dependency injection
- **Event-Driven**: Systems communicate via `EventAggregator` pub/sub system
- **Resource-Based**: Game data stored as Godot Resources (`.tres` files)
- **Modular UI**: Control-based UI system with reusable scene-based components
- **Repository Pattern**: Data access encapsulated in repositories with Result<T> return types

### Key Classes

#### GameStateManager
```csharp
public partial class GameStateManager : Node
{
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase, GamePhase> OnPhaseChanged;

    public void StartLiveShow() { /* ... */ }
    public void EndLiveShow() { /* ... */ }
}

// Access via ServiceRegistry
var gameState = ServiceRegistry.Instance.GameStateManager;
```

#### UIManager
```csharp
public partial class UIManager : Node
{
    public void Initialize() {
        CreateCanvasUI();
        // Initialize tabs and UI components
    }
}

// Access via ServiceRegistry
var uiManager = ServiceRegistry.Instance.UIManager;
```

#### CallerQueue
```csharp
public partial class CallerQueue : Node
{
    public bool AddCaller(Caller caller) { /* ... */ }
    public Caller StartScreeningNext() { /* ... */ }
    public bool ApproveCurrentCaller() { /* ... */ }
    public Caller PutNextCallerOnAir() { /* ... */ }
}

// Access via ServiceRegistry
var queue = ServiceRegistry.Instance.CallerRepository;
```

### Adding New Features

1. **Create new interface**: Define `IMyService` interface in appropriate directory
2. **Create implementation**: Implement `IMyService` as a Node
3. **Register in ServiceRegistry**: Add to `RegisterCoreServices()` method
4. **Add to main scene**: Attach script to node in `scenes/Main.tscn`
5. **Access via ServiceRegistry**: Use `ServiceRegistry.Instance.MyService`
6. **Test integration**: Use `DebugHelper` for testing new functionality

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

- **Engine**: Godot 4.x with C# support
- **Architecture**: Service Registry pattern with event-driven systems
- **UI**: Control-based responsive design with scene-based panels
- **Data**: Resource-based storage (`.tres` files)
- **Events**: EventAggregator pub/sub with weak references
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