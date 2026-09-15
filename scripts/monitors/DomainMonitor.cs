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
    public abstract partial class DomainMonitor : Node, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        protected ICallerRepository? _repository;
        private bool _dependenciesReady;

        /// <summary>
        /// Attempts to resolve this monitor's required services. Returns true once
        /// all dependencies are available. Subclasses override to resolve their own
        /// services but MUST return false (not throw) when a required service is not
        /// yet available — monitors may be instantiated before
        /// ServiceProviderRoot.Initialize() has run (e.g. World3D creates
        /// SoundboardMonitor in _Ready, before Main._Ready).
        /// </summary>
        protected virtual bool ResolveDependencies()
        {
            if (_repository == null)
            {
                DependencyInjection.TryGet<ICallerRepository>(this, out _repository);
            }

            return _repository != null;
        }

        public virtual void OnResolved()
        {
            if (_dependenciesReady)
            {
                return;
            }

            _dependenciesReady = ResolveDependencies();
        }

        public override void _Process(double delta)
        {
            if (!_dependenciesReady)
            {
                _dependenciesReady = ResolveDependencies();
                if (!_dependenciesReady)
                {
                    return;
                }
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
