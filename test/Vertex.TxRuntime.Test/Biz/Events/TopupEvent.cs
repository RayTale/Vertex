using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.TxRuntime.Test.Events
{
    [EventName(nameof(TopupEvent))]
    public class TopupEvent : IEvent
    {
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}
