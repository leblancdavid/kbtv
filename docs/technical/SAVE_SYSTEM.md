# KBTV - Save System

## Overview

The save system provides persistent storage for game progress, player resources, equipment upgrades, and inventory. It uses JSON file serialization to Godot's `user://` directory.

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `SaveManager` | Singleton that handles file I/O and coordinates save/load |
| `SaveData` | Root data container with all persistent state |
| `ISaveable` | Interface for components that participate in save/load |

### File Location

The save file is stored in Godot's `user://` directory, which maps to:

```
Windows: %APPDATA%\Godot\app_userdata\KBTV\
macOS:   ~/Library/Application Support/Godot/app_userdata/KBTV/
Linux:   ~/.local/share/godot/app_userdata/KBTV/
HTML5:   IndexedDB (via Godot's persistent storage)
```

File: `user://kbtv_save.json`

## SaveData Structure

```csharp
[Serializable]
public class SaveData
{
    // Version for migration support
    public int Version = 1;
    public string LastSaveTime;
    
    // Progress
    public int CurrentNight = 1;
    public int Money = 500;  // Starting money
    
    // Equipment levels (1-4, keyed by EquipmentType name)
    public SerializableDictionary<string, int> EquipmentLevels;
    
    // Item inventory (keyed by item ID)
    public SerializableDictionary<string, int> ItemQuantities;
    
    // Lifetime stats
    public int TotalCallersScreened = 0;
    public int TotalShowsCompleted = 0;
    public int PeakListenersAllTime = 0;
}
```

### Default Values (New Game)

| Field | Default | Notes |
|-------|---------|-------|
| `CurrentNight` | 1 | First night |
| `Money` | 500 | Starting funds for initial purchases |
| `EquipmentLevels` | All = 1 | Stock equipment |
| `ItemQuantities` | 3 each | Coffee, Water, Sandwich, Whiskey, Cigarette |

## SaveManager API

### Properties

```csharp
// Check if a save file exists
bool HasSave { get; }

// Get the current save data (null if not loaded)
SaveData CurrentSave { get; }

// Check if data has been modified since last save
bool IsDirty { get; }
```

### Methods

```csharp
// Load save from disk (or create new if none exists)
void Load();

// Save current data to disk
void Save();

// Delete save file and reset to defaults
void DeleteSave();

// Create a fresh save with default values
SaveData CreateNewSave();

// Mark data as modified (triggers auto-save check)
void MarkDirty();
```

### Events

```csharp
// Fired after successful save
event Action OnSaveCompleted;

// Fired after successful load
event Action OnLoadCompleted;

// Fired after save is deleted
event Action OnSaveDeleted;

// Fired when save data changes (for UI updates)
event Action OnDataChanged;
```

## Auto-Save Triggers

The game auto-saves at these points:

| Trigger | Location | Notes |
|---------|----------|-------|
| Night End | `GameStateManager.TransitionToPostShow()` | After income calculated |
| Equipment Purchase | `EquipmentManager.PurchaseUpgrade()` | Immediate save |
| Item Purchase | `ItemManager.PurchaseItem()` | When shop implemented |
| Game Quit | `SaveManager.OnApplicationQuit()` | Safety save |

## ISaveable Interface

Components that need to persist data implement `ISaveable`:

```csharp
public interface ISaveable
{
    // Called when SaveManager is about to save
    void OnBeforeSave(SaveData data);
    
    // Called after SaveManager loads data
    void OnAfterLoad(SaveData data);
}
```

### Implementation Example

```csharp
public class ItemManager : SingletonMonoBehaviour<ItemManager>, ISaveable
{
    public void OnBeforeSave(SaveData data)
    {
        data.ItemQuantities = new SerializableDictionary<string, int>();
        foreach (var slot in _inventory)
        {
            data.ItemQuantities[slot.Key] = slot.Value.Quantity;
        }
    }
    
    public void OnAfterLoad(SaveData data)
    {
        if (data.ItemQuantities != null)
        {
            foreach (var kvp in data.ItemQuantities)
            {
                SetItemQuantity(kvp.Key, kvp.Value);
            }
        }
    }
}
```

## Dictionary Serialization

Godot's JSON class supports dictionaries directly. No custom serialization wrapper needed:

```csharp
[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> _keys = new List<TKey>();
    [SerializeField] private List<TValue> _values = new List<TValue>();
    
    private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
    
    // Dictionary-like access
    public TValue this[TKey key] { get => _dictionary[key]; set => _dictionary[key] = value; }
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
    
    // ISerializationCallbackReceiver
    public void OnBeforeSerialize() { /* sync dict to lists */ }
    public void OnAfterDeserialize() { /* sync lists to dict */ }
}
```

## Version Migration

When loading a save with an older version, `SaveManager` runs migration:

```csharp
private SaveData MigrateSave(SaveData oldData)
{
    // Version 1 -> 2 example
    if (oldData.Version < 2)
    {
        // Add new fields with defaults
        oldData.NewFieldAddedInV2 = defaultValue;
        oldData.Version = 2;
    }
    
    return oldData;
}
```

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Corrupted save file | Log error, offer to start new game |
| Missing save file | Create new save with defaults |
| Write failure | Log error, retry once, warn player |
| Version too new | Log error, refuse to load (prevent data loss) |

## File Format

The save file is human-readable JSON for easy debugging:

```json
{
    "Version": 1,
    "LastSaveTime": "2026-01-11T22:30:00Z",
    "CurrentNight": 5,
    "Money": 1250,
    "EquipmentLevels": {
        "PhoneLine": 2,
        "Broadcast": 1
    },
    "ItemQuantities": {
        "coffee": 5,
        "water": 3,
        "sandwich": 2,
        "whiskey": 1,
        "cigarette": 4
    },
    "TotalCallersScreened": 23,
    "TotalShowsCompleted": 4,
    "PeakListenersAllTime": 847
}
```

## Integration

### Initialization Order

```
GameBootstrap.SetupManagers()
    1. SaveManager.Initialize()     // First - loads save data
    2. EconomyManager.Initialize()  // Reads Money from save
    3. EquipmentManager.Initialize() // Reads equipment levels
    4. ItemManager.Initialize()      // Reads item quantities
    5. ... other managers
```

### Usage in Managers

```csharp
// Reading on init
void Start()
{
    var save = SaveManager.Instance.CurrentSave;
    _money = save.Money;
}

// Writing changes
void SpendMoney(int amount)
{
    _money -= amount;
    SaveManager.Instance.CurrentSave.Money = _money;
    SaveManager.Instance.MarkDirty();
}
```

## Testing

### Debug Commands

| Command | Description |
|---------|-------------|
| `SaveManager.Instance.Save()` | Force save |
| `SaveManager.Instance.DeleteSave()` | Reset to new game |
| `SaveManager.Instance.CurrentSave.Money = 9999` | Cheat money |

### Save File Location (Development)

In Unity Editor, saves go to:
```
C:\Users\{User}\AppData\LocalLow\DefaultCompany\kbtv\kbtv_save.json
```

## Future Considerations

- **Multiple save slots**: Change filename to include slot number
- **Cloud saves**: Abstract file I/O behind interface for Steam Cloud
- **Save encryption**: Optional obfuscation for release builds
- **Autosave indicator**: UI feedback when saving

## References

- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Money and income
- [EQUIPMENT_SYSTEM.md](EQUIPMENT_SYSTEM.md) - Equipment upgrades
- [GAME_DESIGN.md](GAME_DESIGN.md) - Overall game design
