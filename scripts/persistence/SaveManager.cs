using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using KBTV.Core;
using KBTV.Data;

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
        private const int CURRENT_VERSION = 2;

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
            // Removed RegisterSelf - now registered in ServiceProviderRoot
        }

        /// <summary>
        /// Initialize the SaveManager.
        /// </summary>
        public void Initialize()
        {
            // SaveManager doesn't need special initialization
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
                    Log.Error($"[SaveManager] Failed to open save file: {Godot.FileAccess.GetOpenError()}");
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
                            Log.Error("[SaveManager] Failed to parse JSON");
                            _currentSave = CreateNewSave();
                        }
                        else
                        {
                            // Deserialize JSON to SaveData
                            var dict = (Godot.Collections.Dictionary)jsonParse;
                            _currentSave = new SaveData
                            {
                                Version = (int)(long)dict["Version"],
                                LastSaveTime = dict["LastSaveTime"].ToString(),
                                CurrentNight = (int)(long)dict["CurrentNight"],
                                Money = (int)(long)dict["Money"],
                                ShowDurationMinutes = dict.ContainsKey("ShowDurationMinutes") ? (int)(long)dict["ShowDurationMinutes"] : 10,
                                EquipmentLevels = dict.ContainsKey("EquipmentLevels") && dict["EquipmentLevels"].VariantType == Variant.Type.Dictionary
                                    ? ConvertToSystemDictionary((Godot.Collections.Dictionary)dict["EquipmentLevels"])
                                    : new System.Collections.Generic.Dictionary<string, int> { ["PhoneLine"] = 1, ["Broadcast"] = 1 },
                                ItemQuantities = dict.ContainsKey("ItemQuantities") && dict["ItemQuantities"].VariantType == Variant.Type.Dictionary
                                    ? ConvertToSystemDictionary((Godot.Collections.Dictionary)dict["ItemQuantities"])
                                    : new System.Collections.Generic.Dictionary<string, int> {
                                        ["coffee"] = 3, ["water"] = 3, ["sandwich"] = 3, ["whiskey"] = 3, ["cigarette"] = 3
                                    },
                                TotalCallersScreened = (int)(long)dict["TotalCallersScreened"],
                                TotalShowsCompleted = (int)(long)dict["TotalShowsCompleted"],
                                PeakListenersAllTime = (int)(long)dict["PeakListenersAllTime"],
                                TopicXPs = dict.ContainsKey("TopicXPs") && dict["TopicXPs"].VariantType == Variant.Type.Array
                                    ? ConvertToTopicXPList((Godot.Collections.Array)dict["TopicXPs"])
                                    : new List<SaveData.TopicXPData>()
                            };
                        }

                        // Handle version migration
                        if (_currentSave.Version < CURRENT_VERSION)
                        {
                            _currentSave = MigrateSave(_currentSave);
                            _isDirty = true;
                        }
                        else if (_currentSave.Version > CURRENT_VERSION)
                        {
                            Log.Error($"[SaveManager] Save file is from a newer version ({_currentSave.Version} > {CURRENT_VERSION}). Cannot load.");
                            _currentSave = CreateNewSave();
                        }

                        Log.Debug($"[SaveManager] Loaded save from {path} (Night {_currentSave.CurrentNight}, ${_currentSave.Money})");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[SaveManager] Failed to load save: {e.Message}");
                        _currentSave = CreateNewSave();
                    }
                }
            }
            else
            {
                _currentSave = CreateNewSave();
            }

            // Notify saveables
            NotifyLoadComplete();

            _isDirty = false;
            EmitSignal("LoadCompleted");
        }

        /// <summary>
        /// Migrate save data from older versions to current.
        /// </summary>
        private SaveData MigrateSave(SaveData oldData)
        {
            var migrated = oldData;

            // Version 1 -> 2: Add ShowDurationMinutes default
            if (oldData.Version < 2)
            {
                migrated.ShowDurationMinutes = 10; // Default value
                migrated.Version = 2;
                Log.Debug("[SaveManager] Migrated save from version 1 to 2");
            }

            return migrated;
        }

        /// <summary>
        /// Save current data to disk.
        /// </summary>
        public void Save()
        {
            if (_currentSave == null)
            {
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
                    Log.Error($"[SaveManager] Failed to create save file: {Godot.FileAccess.GetOpenError()}");
                    return;
                }

                // Serialize SaveData to JSON
                var dict = new Godot.Collections.Dictionary
                {
                    ["Version"] = _currentSave.Version,
                    ["LastSaveTime"] = _currentSave.LastSaveTime,
                    ["CurrentNight"] = _currentSave.CurrentNight,
                    ["Money"] = _currentSave.Money,
                    ["ShowDurationMinutes"] = _currentSave.ShowDurationMinutes,
                    ["EquipmentLevels"] = ConvertToGodotDictionary(_currentSave.EquipmentLevels),
                    ["ItemQuantities"] = ConvertToGodotDictionary(_currentSave.ItemQuantities),
                    ["TotalCallersScreened"] = _currentSave.TotalCallersScreened,
                    ["TotalShowsCompleted"] = _currentSave.TotalShowsCompleted,
                    ["PeakListenersAllTime"] = _currentSave.PeakListenersAllTime,
                    ["TopicXPs"] = ConvertToGodotArray(_currentSave.TopicXPs)
                };

                string json = Json.Stringify(dict);
                file.StoreString(json);
                file.Close();

                _isDirty = false;
                Log.Debug($"[SaveManager] Saved to {path}");
                EmitSignal("SaveCompleted");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveManager] Failed to save: {e.Message}");
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
                    Log.Debug("[SaveManager] Save file deleted");
                }
                else
                {
                    Log.Error("[SaveManager] Failed to access user directory for deletion");
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
        /// Converts Godot.Collections.Array of TopicXP dictionaries to List<SaveData.TopicXPData>
        /// </summary>
        private List<SaveData.TopicXPData> ConvertToTopicXPList(Godot.Collections.Array godotArray)
        {
            var topicXPList = new List<SaveData.TopicXPData>();
            foreach (var item in godotArray)
            {
                var dict = (Godot.Collections.Dictionary)item;
                var topicXP = new SaveData.TopicXPData
                {
                    TopicId = (string)dict["TopicId"],
                    XP = (float)(double)dict["XP"],  // Godot stores floats as double
                    HighestTierReached = (XPTier)(int)(long)dict["HighestTierReached"]
                };
                topicXPList.Add(topicXP);
            }
            return topicXPList;
        }

        /// <summary>
        /// Converts List<SaveData.TopicXPData> to Godot.Collections.Array of dictionaries
        /// </summary>
        private Godot.Collections.Array ConvertToGodotArray(List<SaveData.TopicXPData> topicXPList)
        {
            var godotArray = new Godot.Collections.Array();
            foreach (var topicXP in topicXPList)
            {
                var dict = new Godot.Collections.Dictionary
                {
                    ["TopicId"] = topicXP.TopicId,
                    ["XP"] = topicXP.XP,
                    ["HighestTierReached"] = (int)topicXP.HighestTierReached
                };
                godotArray.Add(dict);
            }
            return godotArray;
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
                    Log.Error($"[SaveManager] Error in OnBeforeSave for {saveable.GetType().Name}: {e.Message}");
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
                    Log.Error($"[SaveManager] Error in OnAfterLoad for {saveable.GetType().Name}: {e.Message}");
                }
            }
        }


    }
}