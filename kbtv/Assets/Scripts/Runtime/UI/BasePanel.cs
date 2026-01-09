using UnityEngine;

namespace KBTV.UI
{
    /// <summary>
    /// Base class for UI panels that need to subscribe to singleton events.
    /// Handles late-binding pattern for singletons that may not exist in Start().
    /// </summary>
    public abstract class BasePanel : MonoBehaviour
    {
        private bool _isSubscribed = false;

        protected virtual void Start()
        {
            TrySubscribe();
            if (_isSubscribed)
            {
                UpdateDisplay();
            }
        }

        protected virtual void Update()
        {
            if (!_isSubscribed)
            {
                TrySubscribe();
                if (_isSubscribed)
                {
                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// Attempts to subscribe to singleton events.
        /// </summary>
        protected void TrySubscribe()
        {
            if (_isSubscribed) return;
            if (DoSubscribe())
            {
                _isSubscribed = true;
            }
        }

        /// <summary>
        /// Subscribe to singleton events. Return true if successful (singletons available).
        /// </summary>
        protected abstract bool DoSubscribe();

        /// <summary>
        /// Unsubscribe from events. Called in OnDestroy.
        /// </summary>
        protected abstract void DoUnsubscribe();

        /// <summary>
        /// Update the panel's visual state.
        /// </summary>
        protected abstract void UpdateDisplay();

        protected virtual void OnDestroy()
        {
            if (_isSubscribed)
            {
                DoUnsubscribe();
            }
        }
    }
}
