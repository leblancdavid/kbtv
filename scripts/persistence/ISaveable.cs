namespace KBTV.Persistence
{
    /// <summary>
    /// Interface for components that participate in the save/load system.
    /// Implement this to have your manager's data persisted.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Called before SaveManager writes to disk.
        /// Write your data to the SaveData object.
        /// </summary>
        /// <param name="data">The save data container to write to</param>
        void OnBeforeSave(SaveData data);

        /// <summary>
        /// Called after SaveManager loads from disk.
        /// Read your data from the SaveData object.
        /// </summary>
        /// <param name="data">The save data container to read from</param>
        void OnAfterLoad(SaveData data);
    }
}