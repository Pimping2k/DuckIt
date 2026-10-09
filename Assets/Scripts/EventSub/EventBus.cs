using System;
using System.Collections.Generic;

namespace EventSub
{
    public class EventBus
    {
        private static readonly Dictionary<Type, ISubscriptionList> _subscribers = new();

        public static void Subscribe<T>(Action<T> callback) where T : struct, IEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new SubscriptionList<T>();
                _subscribers[type] = list;
            }

            ((SubscriptionList<T>)list).Handlers.Add(callback);
        }

        public static void Unsubscribe<T>(Action<T> callback) where T : struct, IEvent
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                var typedList = (SubscriptionList<T>)list;
                typedList.Handlers.Remove(callback);

                if (typedList.Handlers.Count == 0)
                {
                    _subscribers.Remove(typeof(T));
                }
            }
        }
    
        public static void Publish<T>(T payload) where T : struct, IEvent
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                var handlers = ((SubscriptionList<T>)list).Handlers;
            
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    handlers[i]?.Invoke(payload);
                }
            }
        }
    }
}