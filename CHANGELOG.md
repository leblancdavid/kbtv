# KBTV Changelog

All notable changes to the KBTV project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-14 - Godot Migration Complete

### Added
- **Complete Godot 4.5.1 Migration**: Full port from Unity to Godot engine
- **Core Game Systems**:
  - GameStateManager with phase transitions (PreShow → LiveShow → PostShow)
  - TimeManager with show countdown and tick events
  - ListenerManager with audience tracking and VIBE-based growth
  - CallerQueue with complete caller lifecycle management
  - CallerGenerator with weighted random caller creation
  - EconomyManager with money tracking and transactions
  - SaveManager with JSON persistence framework

- **Vern Character System**:
  - VernStats with comprehensive stat management (Energy, Spirit, Alertness, etc.)
  - Mood-based dialogue variations (Neutral, Tired, Energized, etc.)
  - Stat decay system with configurable multipliers
  - Caller effect system (good/bad caller impacts)

- **UI System (Complete Rewrite)**:
  - UIManagerBootstrap with runtime UI generation
  - TabController with scrollable content areas (CALLERS/ITEMS/STATS)
  - Live header bar with clock, listeners, and on-air indicator
  - Footer panels for transcript, ad breaks, and money display
  - InputHandler with keyboard controls (Y/N/S/Space/E)
  - DebugHelper with comprehensive testing utilities

- **Caller Management System**:
  - Caller class with revelation system and property screening
  - Topic-based screening rules with rule evaluation
  - Caller state machine (Incoming → Screening → OnHold → OnAir)
  - Patience system with automatic disconnection

- **Data & Resources**:
  - Resource-based data storage (Godot .tres files)
  - StatModifier system for item effects
  - EquipmentConfig with upgrade definitions
  - AdData with revenue calculations
  - Dialogue system framework with mood variants

- **Audio Framework** (Placeholders for future implementation):
  - BumperConfig for station audio bumpers
  - TransitionMusicConfig for show transitions
  - AudioStream integration points

- **Persistence System**:
  - JSON-based save/load with SaveData structure
  - ISaveable interface for component serialization
  - SerializableDictionary for complex data types

- **Testing & Debugging Infrastructure**:
  - DebugHelper with manual game state control
  - Comprehensive TESTING_GUIDE.md
  - F12 debug state display
  - Manual caller spawning and state manipulation

### Technical Implementation
- **SingletonNode<T> Pattern**: Custom singleton implementation for Godot
- **Event-Driven Architecture**: C# events with Godot signal integration
- **Control-Based UI**: Responsive Godot UI system replacing Unity Canvas
- **Resource Management**: Proper Godot resource lifecycle management
- **Cross-Platform**: Configured for Windows, macOS, Linux export

### Configuration
- **Input Actions**: Keyboard controls defined in project.godot
- **Viewport**: 1920x1080 reference resolution
- **C# Support**: Full .NET 6.0+ integration
- **Export Presets**: Ready for multiple platform deployment

### Documentation
- **README.md**: Complete setup and usage guide
- **API_DOCUMENTATION.md**: Detailed system API reference
- **DEVELOPER_GUIDE.md**: Extension and modification guide
- **TESTING_GUIDE.md**: Comprehensive testing procedures

### Migration Achievements
- **200+ Lines of Unity UI** → **Complete Godot Control System**
- **Unity Input System** → **Godot Input Actions**
- **Unity ScriptableObjects** → **Godot Resources**
- **Unity Events** → **Godot Signals + C# Events**
- **Unity Canvas** → **Godot Control Nodes**

### Quality Assurance
- **Full Game Loop**: Caller generation → screening → broadcasting → audience response
- **Real-time UI**: All displays update dynamically with game state
- **Input Handling**: Complete keyboard control mapping
- **Debug Tools**: Extensive testing capabilities
- **Performance**: Optimized for 60 FPS gameplay

### Future Roadmap
- Audio system implementation
- Expanded dialogue content
- UI animations and polish
- Additional game features
- Mobile platform support

---

## Migration Notes

This release represents the successful completion of a major game engine migration from Unity to Godot 4.5.1. The project maintains all core functionality while leveraging Godot's strengths for better performance, smaller build sizes, and improved development workflow.

### Breaking Changes
- Complete engine migration (Unity → Godot)
- All APIs redesigned for Godot patterns
- Asset pipeline changes (Unity → Godot resources)

### Compatibility
- **Godot Version**: 4.5.1 or later required
- **C# Version**: .NET 6.0+ required
- **Platforms**: Windows, macOS, Linux supported

---

## Contributing

KBTV now uses gitflow for organized development. See `GITFLOW.md` for branching guidelines and `AGENTS.md` for development best practices.

### Development Workflow
- Use gitflow for all feature development and bug fixes
- Follow conventional commits for clear change tracking
- Create pull requests for all changes to develop branch

## Acknowledgments

- **Unity Foundation**: Original game systems and design
- **Godot Community**: Engine and documentation resources
- **Migration Tools**: Custom development tools created during migration

---

*This changelog documents the complete migration from Unity to Godot. Future updates will follow standard versioning practices.*