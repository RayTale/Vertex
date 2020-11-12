using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Transfer.Grains.Events
{
    [EventName(nameof(CreateEvent))]
    public class CreateEvent : IEvent
    {
        public decimal Balance { get; set; }
    }
}
