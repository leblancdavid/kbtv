using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using KBTV.Core;

namespace KBTV.Persistence
{
    /// <summary>
    /// Manages game save/load functionality.
    /// Saves to user:// as JSON.
    /// </summary>
    public partial class SaveManager : SingletonNode<SaveManager>
    {
        private const string SAVE_FILENAME = "kbtv_save.json";
        private const int CURRENT_VERSION = 1;

        private SaveData _currentSave;
        private bool _isDirty;
        private List<ISaveable> _saveables = new List<ISaveable>();

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

        /// <summary>Fired after successful save.</summary>
        public event Action OnSaveCompleted;

        /// <summary>Fired after successful load.</summary>
        public event Action OnLoadCompleted;

        /// <summary>Fired after save is deleted.</summary>
        public event Action OnSaveDeleted;

        /// <summary>Fired when save data changes.</summary>
        public event Action OnDataChanged;

        // ─────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────

        protected override void OnSingletonReady()
        {
            // No equivalent to DontDestroyOnLoad in Godot for autoloads
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
                        if (jsonParse.Equals(null))
                        {
                            GD.PrintErr("[SaveManager] Failed to parse JSON");
                            _currentSave = CreateNewSave();
                        }
                        else
                        {
                            // TODO: Implement proper JSON deserialization for SaveData
                            // For now, create new save
                            _currentSave = CreateNewSave();
                            GD.Print("[SaveManager] JSON parsing not yet implemented, created new save");
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
            OnLoadCompleted?.Invoke();
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
                // TODO: Implement proper JSON serialization for SaveData
                // For now, just save a placeholder
                var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    GD.PrintErr($"[SaveManager] Failed to create save file: {Godot.FileAccess.GetOpenError()}");
                    return;
                }

                file.StoreString("{}"); // Placeholder JSON
                file.Close();

                _isDirty = false;
                GD.Print($"[SaveManager] Saved to {path}");
                OnSaveCompleted?.Invoke();
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
            OnSaveDeleted?.Invoke();
        }

        /// <summary>
        /// Create a fresh save with default values.
        /// </summary>
        public SaveData CreateNewSave()
        {
            return SaveData.CreateNew();
        }

        /// <summary>
        /// Mark save data as modified. Triggers auto-save checks.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            OnDataChanged?.Invoke();
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