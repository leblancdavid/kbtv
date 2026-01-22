using System;
using Godot;
using KBTV.Ads;
using KBTV.Core;
using KBTV.Managers;

namespace KBTV.Ads
{
    /// <summary>
    /// Handles break execution logic including penalties, listener effects, and state management.
    /// Extracted from AdManager to improve modularity.
    /// </summary>
    public class BreakLogic
    {
        public void ApplyUnqueuedPenalty()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            if (vernStats != null)
            {
                vernStats.Patience.Modify(-AdConstants.UNQUEUED_MOOD_PENALTY);
                GD.Print($"AdManager: Applied {AdConstants.UNQUEUED_MOOD_PENALTY} mood penalty (break not queued)");
            }
        }

        public void ApplyListenerDip()
        {
            var listenerMgr = ServiceRegistry.Instance.ListenerManager;
            if (listenerMgr != null)
            {
                int current = listenerMgr.CurrentListeners;
                int dip = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
                listenerMgr.ModifyListeners(-dip);
            }
        }

        public void RestoreListeners()
        {
            var listenerMgr = ServiceRegistry.Instance.ListenerManager;
            if (listenerMgr != null)
            {
                int current = listenerMgr.CurrentListeners;
                int restore = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
                listenerMgr.ModifyListeners(restore);
            }
        }
    }
}