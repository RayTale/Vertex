using System;

namespace Vertex.Abstractions.Event
{
    public interface IEventNameGenerator
    {
        string GetName(Type eventType);
    }
}
