using Orleans;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(ErrorTestEvent))]
    [GenerateSerializer]
    public class ErrorTestEvent : IEvent
    {
    }
}
