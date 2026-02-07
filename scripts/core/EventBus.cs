#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using KBTV.Core;

namespace KBTV.Core
{
    /// <summary>
    /// Global event bus for decoupled inter-system communication.
    /// Allows systems to publish and subscribe to game events without direct dependencies.
    /// Automatically defers event publishing to main thread when called from background threads.
    /// </summary>
    public partial class EventBus : Node
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        // Temporary storage for events that need to be published on main thread
        private GameEvent? _deferredEvent;

        // Main thread ID for thread checking
        private int _mainThreadId;

        public override void _Ready()
        {
            // Store main thread ID for thread checking
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// If called from background thread, defers publishing to main thread.
        /// </summary>
        /// <param name="gameEvent">The event to publish</param>
        public void Publish(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                Log.Error("EventBus.Publish: Attempted to publish null event");
                return;
            }

            // If we're on a background thread, defer to main thread
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                _deferredEvent = gameEvent;
                CallDeferred(nameof(PublishDeferred));
                return;
            }

            // On main thread - publish immediately
            PublishImmediate(gameEvent);
        }

        /// <summary>
        /// Deferred method called on main thread to publish stored event.
        /// </summary>
        private void PublishDeferred()
        {
            if (_deferredEvent != null)
            {
                PublishImmediate(_deferredEvent);
                _deferredEvent = null;
            }
        }

        /// <summary>
        /// Internal method to publish event immediately (assumes main thread).
        /// </summary>
        private void PublishImmediate(GameEvent gameEvent)
        {
            var eventType = gameEvent.GetType();

            if (_subscribers.TryGetValue(eventType, out var subscribers))
            {
                foreach (var subscriber in subscribers.ToArray())
                {
                    try
                    {
                        subscriber.DynamicInvoke(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"EventBus.Publish: Error invoking subscriber for {eventType.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Subscribe to events of type T.
        /// </summary>
        /// <param name="handler">The event handler</param>
        public void Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            _subscribers[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from events of type T.
        /// </summary>
        /// <param name="handler">The event handler to remove</param>
        public void Unsubscribe<T>(Action<T> handler) where T : GameEvent
        {
            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var subscribers))
            {
                subscribers.Remove(handler);
            }
        }

        /// <summary>
        /// Clear all subscribers. Useful for cleanup.
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}