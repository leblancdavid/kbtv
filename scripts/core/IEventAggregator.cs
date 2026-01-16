using System;
using System.Collections.Generic;

namespace KBTV.Core
{
    /// <summary>
    /// Lightweight event aggregator for cross-component communication.
    /// Reduces direct signal coupling between components.
    /// Subscribe at startup and publish events from anywhere.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Subscribe to an event type.
        /// </summary>
        void Subscribe<TEvent>(object subscriber, Action<TEvent> handler);

        /// <summary>
        /// Unsubscribe from all events for a subscriber.
        /// </summary>
        void Unsubscribe(object subscriber);

        /// <summary>
        /// Unsubscribe from a specific event type.
        /// </summary>
        void Unsubscribe<TEvent>(object subscriber);

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        void Publish<TEvent>(TEvent eventData);

        /// <summary>
        /// Check if a subscriber is subscribed to any events.
        /// </summary>
        bool IsSubscribed(object subscriber);
    }
}
