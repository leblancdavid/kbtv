#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.Monitors
{
    /// <summary>
    /// Abstract base class for domain-specific monitors that handle state updates.
    /// Monitors run in the scene tree's _Process() loop, updating their domain's
    /// state values each frame and triggering state-driven side effects.
    ///
    /// Pattern:
    /// - Subclasses handle ONE domain (Callers, VernStats, etc.)
    /// - Only update state values (wait time, stat decay, etc.)
    /// - Trigger events for state-driven side effects (disconnection, depletion)
    /// - Do NOT handle UI updates, persistence, or business logic
    /// </summary>
    public abstract partial class DomainMonitor : Node
    {
        protected ICallerRepository? _repository;

        public override void _Ready()
        {
            if (ServiceRegistry.IsInitialized)
            {
                _repository = ServiceRegistry.Instance.CallerRepository;
            }
        }

        public override void _Process(double delta)
        {
            if (_repository == null)
            {
                return;
            }

            OnUpdate((float)delta);
        }

        /// <summary>
        /// Override to implement domain-specific update logic.
        /// Called every frame when the monitor is active.
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds since last frame</param>
        protected abstract void OnUpdate(float deltaTime);

        /// <summary>
        /// Override to handle event-driven updates.
        /// Called when relevant game events are published.
        /// Default implementation does nothing - override in subclasses that need event handling.
        /// </summary>
        /// <param name="gameEvent">The game event to process</param>
        public virtual void OnEvent(GameEvent gameEvent)
        {
            // Default: no-op. Subclasses override for event-driven behavior.
        }
    }
}
