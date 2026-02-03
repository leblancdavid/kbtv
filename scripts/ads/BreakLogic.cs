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
        private readonly GameStateManager _gameStateManager;
        private readonly ListenerManager _listenerManager;

        public BreakLogic(GameStateManager gameStateManager, ListenerManager listenerManager)
        {
            _gameStateManager = gameStateManager;
            _listenerManager = listenerManager;
        }

        /// <summary>
        /// Apply penalty when break is not queued properly.
        /// Affects Emotional stat (replacing old Patience reference).
        /// </summary>
        public void ApplyUnqueuedPenalty()
        {
            var vernStats = _gameStateManager?.VernStats;
            if (vernStats != null)
            {
                // Apply to Emotional stat (mood penalty)
                vernStats.Emotional.Modify(-AdConstants.UNQUEUED_MOOD_PENALTY);
                GD.Print($"AdManager: Applied {AdConstants.UNQUEUED_MOOD_PENALTY} mood penalty (break not queued)");
            }
        }

        public void ApplyListenerDip()
        {
            int current = _listenerManager.CurrentListeners;
            int dip = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
            _listenerManager.ModifyListeners(-dip);
        }

        public void RestoreListeners()
        {
            int current = _listenerManager.CurrentListeners;
            int restore = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
            _listenerManager.ModifyListeners(restore);
        }
    }
}
