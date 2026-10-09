using System;
using System.Collections.Generic;

namespace EventSub
{
    public class SubscriptionList<T> : ISubscriptionList
    {
        public readonly List<Action<T>> Handlers = new();
    }
}