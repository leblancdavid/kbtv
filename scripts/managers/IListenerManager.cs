using System;
using Godot;

namespace KBTV.Managers
{
    public interface IListenerManager
    {
        int CurrentListeners { get; }
        int PeakListeners { get; }
        int StartingListeners { get; }
        int ListenerChange { get; }

        event Action<int, int> ListenersChanged;
        event Action<int> PeakReached;

        void ModifyListeners(int amount);
        string GetFormattedListeners();
        string GetFormattedChange();
    }
}
