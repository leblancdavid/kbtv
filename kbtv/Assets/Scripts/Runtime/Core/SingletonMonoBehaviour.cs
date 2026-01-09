using UnityEngine;

namespace KBTV.Core
{
    /// <summary>
    /// Base class for singleton MonoBehaviours.
    /// Handles Instance property and duplicate destruction automatically.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)(object)this;
            OnSingletonAwake();
        }

        /// <summary>
        /// Called after singleton is established. Override instead of Awake().
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
