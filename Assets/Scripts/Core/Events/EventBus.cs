using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Core.Events
{
    // Global publish/subscribe channel that lets gameplay systems react to each other
    // without holding direct references
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlersByEventType = new Dictionary<Type, Delegate>();

        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            handlersByEventType.TryGetValue(eventType, out Delegate existingHandlers);
            handlersByEventType[eventType] = Delegate.Combine(existingHandlers, handler);
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            Type eventType = typeof(TEvent);

            if (handler == null || !handlersByEventType.TryGetValue(eventType, out Delegate existingHandlers))
            {
                return;
            }

            Delegate remainingHandlers = Delegate.Remove(existingHandlers, handler);

            if (remainingHandlers == null)
            {
                handlersByEventType.Remove(eventType);
            }
            else
            {
                handlersByEventType[eventType] = remainingHandlers;
            }
        }

        public static void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
        {
            if (!handlersByEventType.TryGetValue(typeof(TEvent), out Delegate handlers))
            {
                return;
            }

            // Handlers can subscribe/unsubscribe while publishing
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<TEvent>)handler).Invoke(gameEvent);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static void Clear()
        {
            handlersByEventType.Clear();
        }

        // For disabled domain reload
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            Clear();
        }
    }
}
