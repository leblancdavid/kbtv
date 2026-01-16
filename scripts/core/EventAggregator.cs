using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KBTV.Core
{
    /// <summary>
    /// Implementation of IEventAggregator using concurrent dictionaries for thread safety.
    /// </summary>
    public partial class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<WeakReference<Action<object>>>> _subscriptions = new();
        private readonly ConcurrentDictionary<int, HashSet<Type>> _subscriberTypes = new();
        private readonly Dictionary<(int subscriberId, Type eventType), WeakReference<Action<object>>> _handlerReferences = new();

        public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        {
            if (subscriber == null)
            {
                GD.PrintErr("EventAggregator: Cannot subscribe with null subscriber");
                return;
            }

            if (handler == null)
            {
                GD.PrintErr("EventAggregator: Cannot subscribe with null handler");
                return;
            }

            var eventType = typeof(TEvent);
            var subscriberId = subscriber.GetHashCode();

            var actionWrapper = new Action<object>(obj =>
            {
                if (obj is TEvent typedEvent)
                {
                    handler(typedEvent);
                }
            });

            var subscriptions = _subscriptions.GetOrAdd(eventType, _ => new List<WeakReference<Action<object>>>());
            lock (subscriptions)
            {
                subscriptions.Add(new WeakReference<Action<object>>(actionWrapper));
            }

            lock (_handlerReferences)
            {
                _handlerReferences[(subscriberId, eventType)] = new WeakReference<Action<object>>(actionWrapper);
            }

            _subscriberTypes.GetOrAdd(subscriberId, _ => new HashSet<Type>()).Add(eventType);

            GD.Print($"EventAggregator: Subscriber {subscriber.GetType().Name} subscribed to {eventType.Name}");
        }

        public void Unsubscribe(object subscriber)
        {
            if (subscriber == null)
            {
                return;
            }

            var subscriberId = subscriber.GetHashCode();

            if (_subscriberTypes.TryRemove(subscriberId, out var types))
            {
                foreach (var eventType in types)
                {
                    lock (_subscriptions)
                    {
                        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
                        {
                            var key = (subscriberId, eventType);
                            if (_handlerReferences.TryGetValue(key, out var handlerRef) && handlerRef.TryGetTarget(out var handler))
                            {
                                subscriptions.RemoveAll(r => r.TryGetTarget(out var existing) && existing == handler);
                            }
                            _handlerReferences.Remove(key);
                        }
                    }
                }
                GD.Print($"EventAggregator: Unsubscribed {subscriber.GetType().Name} from all events");
            }
        }

        public void Unsubscribe<TEvent>(object subscriber)
        {
            if (subscriber == null)
            {
                return;
            }

            var eventType = typeof(TEvent);
            var subscriberId = subscriber.GetHashCode();

            lock (_subscriptions)
            {
                if (_subscriptions.TryGetValue(eventType, out var subscriptions))
                {
                    var key = (subscriberId, eventType);
                    if (_handlerReferences.TryGetValue(key, out var handlerRef) && handlerRef.TryGetTarget(out var handler))
                    {
                        subscriptions.RemoveAll(r => r.TryGetTarget(out var existing) && existing == handler);
                    }
                    _handlerReferences.Remove(key);
                }
            }

            _subscriberTypes.AddOrUpdate(subscriberId,
                _ => new HashSet<Type>(),
                (_, existing) =>
                {
                    lock (existing)
                    {
                        existing.Remove(eventType);
                    }
                    return existing;
                });

            GD.Print($"EventAggregator: Unsubscribed {subscriber.GetType().Name} from {eventType.Name}");
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);

            if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            {
                return;
            }

            var handlersToRemove = new List<WeakReference<Action<object>>>();
            int handlerCount = 0;

            lock (subscriptions)
            {
                foreach (var weakRef in subscriptions)
                {
                    if (weakRef.TryGetTarget(out var handler))
                    {
                        handler(eventData);
                        handlerCount++;
                    }
                    else
                    {
                        handlersToRemove.Add(weakRef);
                    }
                }

                foreach (var deadRef in handlersToRemove)
                {
                    subscriptions.Remove(deadRef);
                }
            }

            if (handlerCount == 0)
            {
                GD.Print($"EventAggregator: No handlers for {eventType.Name}");
            }
        }

        public bool IsSubscribed(object subscriber)
        {
            if (subscriber == null)
            {
                return false;
            }

            var subscriberId = subscriber.GetHashCode();
            return _subscriberTypes.TryGetValue(subscriberId, out var types) && types.Count > 0;
        }
    }
}
