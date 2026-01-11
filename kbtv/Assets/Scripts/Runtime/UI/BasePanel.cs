using UnityEngine;

namespace KBTV.UI
{
    /// <summary>
    /// Base class for UI panels that need to subscribe to singleton events.
    /// Handles late-binding pattern for singletons that may not exist in Start().
    /// Also handles panels that are initially inactive (whose Start() won't run until activated).
    /// </summary>
    public abstract class BasePanel : MonoBehaviour
    {
        private bool _isSubscribed = false;
        private bool _isUIBuilt = false; // Set by derived classes after BuildUI() completes

        /// <summary>
        /// Call this at the end of BuildUI() in derived classes to enable subscription.
        /// </summary>
        protected void MarkUIBuilt()
        {
            _isUIBuilt = true;
            Debug.Log($"{GetType().Name}: MarkUIBuilt called");
        }

        protected virtual void Start()
        {
            Debug.Log($"{GetType().Name}: Start called, _isUIBuilt={_isUIBuilt}");
            TrySubscribe();
            if (_isSubscribed)
            {
                UpdateDisplay();
            }
        }

        protected virtual void OnEnable()
        {
            Debug.Log($"{GetType().Name}: OnEnable called, _isUIBuilt={_isUIBuilt}, _isSubscribed={_isSubscribed}");
            // Only try to subscribe if UI has been built.
            // OnEnable is called during AddComponent, before BuildUI() runs.
            if (!_isUIBuilt) return;
            
            // Try to subscribe when enabled - this handles panels that are initially inactive
            // (Start() only runs once when first activated, but OnEnable runs every time)
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
                    Debug.Log($"{GetType().Name}: Subscribed via Update");
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
            if (!_isUIBuilt) return; // Don't subscribe before UI is ready
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
