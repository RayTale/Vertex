using System;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Event
{
    public class EventNameGenerator : IEventNameGenerator
    {
        public string GetName(Type eventType)
        {
            return eventType.Name;
        }
    }
}
