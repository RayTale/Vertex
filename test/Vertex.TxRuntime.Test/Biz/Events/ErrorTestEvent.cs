using Vertex.Abstractions.Event;

namespace Vertex.TxRuntime.Test.Events
{
    [EventName(nameof(ErrorTestEvent))]
    public class ErrorTestEvent : IEvent
    {
    }
}
