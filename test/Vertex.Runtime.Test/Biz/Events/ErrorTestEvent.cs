using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(ErrorTestEvent))]
    public class ErrorTestEvent : IEvent
    {
    }
}
