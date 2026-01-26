#nullable enable

using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Base class for UI components that need initialization.
    /// Provides a standard pattern for component initialization.
    /// </summary>
    public partial class ServiceAwareComponent : Control
    {
        protected bool _isInitialized = false;

        public override void _Ready()
        {
            // Initialize immediately (DI system handles dependencies)
            InitializeWithServices();
        }

        /// <summary>
        /// Override this method to perform initialization.
        /// Called once when the component is ready.
        /// </summary>
        protected virtual void InitializeWithServices()
        {
            _isInitialized = true;
        }
    }
}
