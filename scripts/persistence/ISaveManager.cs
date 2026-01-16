using System;
using Godot;

namespace KBTV.Persistence
{
    public interface ISaveManager
    {
        SaveData CurrentSave { get; }
        bool IsDirty { get; }
        bool HasSave { get; }

        event Action SaveCompleted;
        event Action LoadCompleted;
        event Action SaveDeleted;
        event Action DataChanged;

        void RegisterSaveable(ISaveable saveable);
        void UnregisterSaveable(ISaveable saveable);
        void Load();
        void Save();
        void DeleteSave();
        void MarkDirty();
    }
}
