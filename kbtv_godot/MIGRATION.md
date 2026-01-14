# KBTV Migration: Unity to Godot

## Overview
Migrating the KBTV paranormal radio simulation game from Unity to Godot engine.

**Source**: `../kbtv_unity/` (Unity project)  
**Target**: `./` (Godot project)  
**Engine**: Godot 4.5 (C# support)  
**Scripting**: C# (for easiest porting from Unity C# project)  

## Migration Plan

### Phase 1: Setup & Infrastructure
- [x] Install Godot 4.5
- [x] Create new 2D Godot project
- [x] Set up C# environment
- [ ] Configure project settings (input, physics, etc.)

### Phase 2: Core Architecture
- [x] Convert SingletonMonoBehaviour to SingletonNode
- [x] Port GamePhase enum
- [x] Port GameStateManager
- [x] Port VernStats (complete Resource version with all methods)
- [x] Port VernMoodType, StatType enums
- [x] Port TimeManager
- [x] Implement event system (C# events work in Godot)
- [x] Port ListenerManager
- [x] Port EconomyManager
- [x] Port IncomeCalculator
- [x] Set up save/load system (JSON persistence framework)

### Phase 3: Data & Resources
- [x] Convert ScriptableObjects to Godot Resources:
  - ✅ Topic.cs (caller screening rules)
  - ✅ VernDialogueTemplate.cs (broadcast dialogue templates)
  - ✅ ArcRepository.cs (conversation arc storage)
  - ✅ ConversationArc.cs (arc data structures)
  - ✅ StatModifier.cs (stat modification items)
  - ✅ EquipmentConfig.cs (upgrade definitions)
  - ✅ AdData.cs (advertisement content)
  - ✅ BumperConfig.cs (audio bumper configuration)
  - ✅ TransitionMusicConfig.cs (transition music configuration)
  - ✅ Supporting classes: AdType.cs, EquipmentType.cs, EquipmentUpgrade.cs, DialogueTypes.cs, ArcJsonParser.cs
- [ ] Migrate Topics, Items, VernStats configurations
- [ ] Copy audio files and dialogue JSON
- [ ] Port textures and UI assets

### Phase 4: UI System Recreation
- [x] Recreate runtime UI generation with Godot Control nodes:
  - ✅ UIManagerBootstrap.cs (main UI orchestrator)
  - ✅ TabController.cs (tab system with scrolling content)
  - ✅ UIHelpers.cs (Godot-adapted helper functions)
  - ✅ TabDefinition.cs (tab configuration)
- [x] Port UITheme and styling (basic colors and layout)
- [x] Implement panels: HeaderBar (live indicator, clock, listeners), Footer (OnAir, Transcript, AdBreak, Money), Tab system
- [x] Convert input handling (Unity Input System → Godot Input)

### Phase 5: Game Systems Integration
- [x] **Stats/VIBE**: VernStats ✅, stat modifiers ✅, show quality calculation ✅
- [x] **Callers**: Caller generation ✅, screening logic ✅, queue management ✅
- [ ] **Dialogue**: Arc-based conversations, JSON parsing, voice playback
- [x] **Economy**: Income ✅, upgrades ✅, equipment ✅
- [ ] **Audio**: AudioManager, SFX, voice audio service
- [x] **UI Integration**: CallerQueue display, real-time updates

### Phase 6: Scenes & Testing
- [x] Create main scene (equivalent to SampleScene.unity)
- [x] Set up camera and viewport for 2D game
- [x] Integrate all manager systems (GameState, Time, Listeners, Economy, Callers)
- [x] Add input handling for player controls
- [x] Create debug/testing utilities
- [ ] End-to-end game loop testing

### Phase 7: Testing & Polish
- [ ] Port and adapt unit tests (Unity Test Framework → GUT)
- [ ] Test all game loops: PreShow → LiveShow → PostShow
- [ ] Balance and polish gameplay
- [ ] Performance optimization

## API Mappings

### Lifecycle
- `MonoBehaviour.Awake()` → `Node._Ready()`
- `MonoBehaviour.Start()` → `Node._Ready()` (delayed if needed)
- `MonoBehaviour.Update()` → `Node._Process(delta)`
- `MonoBehaviour.FixedUpdate()` → `Node._PhysicsProcess(delta)`

### Components
- `GameObject` → `Node`
- `Transform` → `Node` properties (position, rotation, scale)
- `Rigidbody2D` → `RigidBody2D`
- `Collider2D` → `CollisionShape2D`

### UI
- `Canvas` → `Control` (CanvasLayer)
- `RectTransform` → `Control` rect properties
- `Button` → `Button`
- `TextMeshPro` → `Label` or `RichTextLabel`
- `Image` → `TextureRect` or `Sprite2D`

### Input
- `Input.GetKey()` → `Input.IsKeyPressed()`
- `Input.GetAxis()` → `Input.GetAxis()`
- Input Actions → Godot Input Map

### Audio
- `AudioSource` → `AudioStreamPlayer`
- Addressables → `ResourceLoader`

### Events
- C# events → Godot signals (custom signals)
- `UnityEvent` → `Signal`

## Challenges & Notes
- Runtime UI generation requires full rewrite (200+ lines)
- Complex event subscriptions need careful signal conversion
- Dialogue system JSON reuse, but playback adaptation
- No direct shader conversion; may need custom Godot shaders
- Testing framework change from Unity to GUT

## Progress Tracking
- **Started**: Jan 14, 2026
- **Phase**: Phase 6: Scenes & Testing In Progress
- **Completed**:
  - ✅ Created Godot project directory with C# support
  - ✅ Generated project.godot with proper dotnet configuration
  - ✅ Set up folder structure (scenes/, scripts/core, scripts/data, scripts/managers, scripts/economy, scripts/persistence)
  - ✅ Ported all core scripts: SingletonNode.cs, GamePhase.cs, Stat.cs, GameStateManager.cs, VernStats.cs (complete), VernMoodType.cs, StatType.cs, TimeManager.cs
  - ✅ Ported game systems: ListenerManager.cs, EconomyManager.cs, IncomeCalculator.cs
  - ✅ Implemented save/load framework: SaveManager.cs, ISaveable.cs, SaveData.cs, SerializableDictionary.cs
  - ✅ Created Main.tscn scene with GameStateManager
  - ✅ Integrated all systems in GameStateManager for end-of-show processing
- **Current State**: Phase 6 in progress, main scene created with input handling
- **Next Steps**: Complete end-to-end testing and performance optimization

## Tools & Resources
- [Unity to Godot Converter](https://github.com/a2937/unity-to-godot-converter) - C# script conversion
- [Unity to Godot Migration Guide](https://uni-dot-migration.vercel.app/) - API cheat sheet
- [Godot Docs](https://docs.godotengine.org/) - Official documentation
- [GUT](https://github.com/bitwes/GUT) - Godot unit testing framework