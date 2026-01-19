using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using KBTV.Core;

namespace KBTV.Persistence
{
    /// <summary>
    /// Manages game save/load functionality.
    /// Saves to user:// as JSON.
    /// </summary>
    public partial class SaveManager : Node
    {
		[Signal] public delegate void SaveCompletedEventHandler();
		[Signal] public delegate void LoadCompletedEventHandler();
		[Signal] public delegate void SaveDeletedEventHandler();
		[Signal] public delegate void DataChangedEventHandler();

        // ─────────────────────────────────────────────────────────────
        // FIELDS
        // ─────────────────────────────────────────────────────────────

        private SaveData _currentSave = new SaveData();
        private bool _isDirty;
        private List<ISaveable> _saveables = new List<ISaveable>();
        private const string SAVE_FILENAME = "save.json";
        private const int CURRENT_VERSION = 1;

        // ─────────────────────────────────────────────────────────────
        // CONSTANTS
        // ─────────────────────────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// The current save data. Null if not loaded.
        /// </summary>
        public SaveData CurrentSave => _currentSave;

        /// <summary>
        /// Whether save data has been modified since last save.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Whether a save file exists on disk.
        /// </summary>
        public bool HasSave => Godot.FileAccess.FileExists(GetSavePath());

        // ─────────────────────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────────────────────



        // ─────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<SaveManager>(this);
        }

        private void HandleTreeExiting()
        {
            // Safety save on exit
            if (_isDirty && _currentSave != null)
            {
                Save();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Registration
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Register a component to participate in save/load.
        /// </summary>
        public void RegisterSaveable(ISaveable saveable)
        {
            if (!_saveables.Contains(saveable))
            {
                _saveables.Add(saveable);
            }
        }

        /// <summary>
        /// Unregister a component from save/load.
        /// </summary>
        public void UnregisterSaveable(ISaveable saveable)
        {
            _saveables.Remove(saveable);
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Load save from disk, or create new if none exists.
        /// </summary>
        public void Load()
        {
            string path = GetSavePath();

            if (Godot.FileAccess.FileExists(path))
            {
                var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"[SaveManager] Failed to open save file: {Godot.FileAccess.GetOpenError()}");
                    _currentSave = CreateNewSave();
                }
                else
                {
                    try
                    {
                        string json = file.GetAsText();
                        file.Close();

                        var jsonParse = Json.ParseString(json);
                        if (jsonParse.VariantType == Variant.Type.Nil)
                        {
                            GD.PrintErr("[SaveManager] Failed to parse JSON");
                            _currentSave = CreateNewSave();
                        }
                        else
                        {
                            // Deserialize JSON to SaveData
                            var dict = (Godot.Collections.Dictionary)jsonParse;
                            _currentSave = new SaveData
                            {
                                Version = (int)(long)dict["Version"],
                                LastSaveTime = (string)dict["LastSaveTime"],
                                CurrentNight = (int)(long)dict["CurrentNight"],
                                Money = (int)(long)dict["Money"],
                                EquipmentLevels = ConvertToSystemDictionary((Godot.Collections.Dictionary)dict["EquipmentLevels"]),
                                ItemQuantities = ConvertToSystemDictionary((Godot.Collections.Dictionary)dict["ItemQuantities"]),
                                TotalCallersScreened = (int)(long)dict["TotalCallersScreened"],
                                TotalShowsCompleted = (int)(long)dict["TotalShowsCompleted"],
                                PeakListenersAllTime = (int)(long)dict["PeakListenersAllTime"]
                            };
                        }

                        // TODO: Handle version migration
                        // if (_currentSave.Version < CURRENT_VERSION)
                        // {
                        //     _currentSave = MigrateSave(_currentSave);
                        //     _isDirty = true;
                        // }
                        // else if (_currentSave.Version > CURRENT_VERSION)
                        // {
                        //     GD.PrintErr($"[SaveManager] Save file is from a newer version ({_currentSave.Version} > {CURRENT_VERSION}). Cannot load.");
                        //     _currentSave = CreateNewSave();
                        // }

                        GD.Print($"[SaveManager] Loaded save from {path} (Night {_currentSave.CurrentNight}, ${_currentSave.Money})");
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr($"[SaveManager] Failed to load save: {e.Message}");
                        _currentSave = CreateNewSave();
                    }
                }
            }
            else
            {
                GD.Print("[SaveManager] No save file found, creating new save");
                _currentSave = CreateNewSave();
            }

            // Notify saveables
            NotifyLoadComplete();

            _isDirty = false;
            EmitSignal("LoadCompleted");
        }

        /// <summary>
        /// Save current data to disk.
        /// </summary>
        public void Save()
        {
            if (_currentSave == null)
            {
                GD.Print("[SaveManager] No save data to save");
                return;
            }

            // Collect data from all saveables
            NotifyBeforeSave();

            // Update timestamp
            _currentSave.LastSaveTime = DateTime.UtcNow.ToString("o");

            string path = GetSavePath();

            try
            {
                var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    GD.PrintErr($"[SaveManager] Failed to create save file: {Godot.FileAccess.GetOpenError()}");
                    return;
                }

                // Serialize SaveData to JSON
                var dict = new Godot.Collections.Dictionary
                {
                    ["Version"] = _currentSave.Version,
                    ["LastSaveTime"] = _currentSave.LastSaveTime,
                    ["CurrentNight"] = _currentSave.CurrentNight,
                    ["Money"] = _currentSave.Money,
                    ["EquipmentLevels"] = ConvertToGodotDictionary(_currentSave.EquipmentLevels),
                    ["ItemQuantities"] = ConvertToGodotDictionary(_currentSave.ItemQuantities),
                    ["TotalCallersScreened"] = _currentSave.TotalCallersScreened,
                    ["TotalShowsCompleted"] = _currentSave.TotalShowsCompleted,
                    ["PeakListenersAllTime"] = _currentSave.PeakListenersAllTime
                };

                string json = Json.Stringify(dict);
                file.StoreString(json);
                file.Close();

                _isDirty = false;
                GD.Print($"[SaveManager] Saved to {path}");
                EmitSignal("SaveCompleted");
            }
            catch (Exception e)
            {
                GD.PrintErr($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Delete save file and reset to defaults.
        /// </summary>
        public void DeleteSave()
        {
            string path = GetSavePath();

            if (Godot.FileAccess.FileExists(path))
            {
                var dir = DirAccess.Open("user://");
                if (dir != null)
                {
                    dir.Remove(path);
                    GD.Print("[SaveManager] Save file deleted");
                }
                else
                {
                    GD.PrintErr("[SaveManager] Failed to access user directory for deletion");
                }
            }

            _currentSave = CreateNewSave();
            NotifyLoadComplete();

            _isDirty = false;
            EmitSignal("SaveDeleted");
        }

        /// <summary>
        /// Creates a new save with default values.
        /// </summary>
        private SaveData CreateNewSave()
        {
            return SaveData.CreateNew();
        }

        /// <summary>
        /// Converts Godot.Collections.Dictionary to System.Collections.Generic.Dictionary<string, int>
        /// </summary>
        private System.Collections.Generic.Dictionary<string, int> ConvertToSystemDictionary(Godot.Collections.Dictionary godotDict)
        {
            var systemDict = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var kv in godotDict)
            {
                systemDict[(string)kv.Key] = (int)(long)kv.Value;
            }
            return systemDict;
        }

        /// <summary>
        /// Converts System.Collections.Generic.Dictionary<string, int> to Godot.Collections.Dictionary
        /// </summary>
        private Godot.Collections.Dictionary ConvertToGodotDictionary(System.Collections.Generic.Dictionary<string, int> systemDict)
        {
            var godotDict = new Godot.Collections.Dictionary();
            foreach (var kv in systemDict)
            {
                godotDict[kv.Key] = kv.Value;
            }
            return godotDict;
        }

        /// <summary>
        /// Mark save data as modified. Triggers auto-save checks.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            EmitSignal("DataChanged");
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private string GetSavePath()
        {
            return SAVE_FILENAME; // Godot resolves "user://" automatically
        }

        private void NotifyBeforeSave()
        {
            foreach (var saveable in _saveables)
            {
                try
                {
                    saveable.OnBeforeSave(_currentSave);
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[SaveManager] Error in OnBeforeSave for {saveable.GetType().Name}: {e.Message}");
                }
            }
        }

        private void NotifyLoadComplete()
        {
            foreach (var saveable in _saveables)
            {
                try
                {
                    saveable.OnAfterLoad(_currentSave);
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[SaveManager] Error in OnAfterLoad for {saveable.GetType().Name}: {e.Message}");
                }
            }
        }

        private SaveData MigrateSave(SaveData oldData)
        {
            // Version migrations go here
            // Example:
            // if (oldData.Version < 2)
            // {
            //     oldData.NewField = defaultValue;
            //     oldData.Version = 2;
            // }

            GD.Print($"[SaveManager] Migrated save from version {oldData.Version} to {CURRENT_VERSION}");
            oldData.Version = CURRENT_VERSION;
            return oldData;
        }
    }
}