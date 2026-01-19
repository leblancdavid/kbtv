#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;

namespace KBTV.Core
{
    /// <summary>
    /// Global event bus for decoupled inter-system communication.
    /// Allows systems to publish and subscribe to game events without direct dependencies.
    /// </summary>
    public partial class EventBus : Node
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<EventBus>(this);
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <param name="gameEvent">The event to publish</param>
        public void Publish(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                GD.PrintErr("EventBus.Publish: Attempted to publish null event");
                return;
            }

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
                        GD.PrintErr($"EventBus.Publish: Error invoking subscriber for {eventType.Name}: {ex.Message}");
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