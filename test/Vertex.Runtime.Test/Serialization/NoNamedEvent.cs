using Orleans;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Serialization
{
    [GenerateSerializer]
    public class NoNamedEvent : IEvent
    {
    }
}
