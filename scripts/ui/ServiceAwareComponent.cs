#nullable enable

using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Base class for UI components that depend on ServiceRegistry being initialized.
    /// Provides a standard pattern for deferred initialization with automatic retry.
    /// </summary>
    public partial class ServiceAwareComponent : Control
    {
        protected bool _isInitialized = false;

        public override void _Ready()
        {
            if (ServiceRegistry.IsInitialized)
            {
                InitializeWithServices();
            }
            else
            {
                CallDeferred(nameof(RetryInitialization));
            }
        }

        protected virtual void RetryInitialization()
        {
            if (ServiceRegistry.IsInitialized)
            {
                InitializeWithServices();
            }
            else
            {
                CallDeferred(nameof(RetryInitialization));
            }
        }

        /// <summary>
        /// Override this method to perform initialization after ServiceRegistry is ready.
        /// Called exactly once when ServiceRegistry becomes available.
        /// </summary>
        protected virtual void InitializeWithServices()
        {
            _isInitialized = true;
        }

        protected bool IsServiceRegistryReady => ServiceRegistry.IsInitialized;
    }
}
