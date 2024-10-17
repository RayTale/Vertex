using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(TopupEvent))]
    public class TopupEvent : IEvent
    {
        public decimal Amount { get; set; }

        public decimal Balance { get; set; }
    }
}
