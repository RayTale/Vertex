using Vertex.Abstractions.Event;

namespace Vertex.TxRuntime.Test.Events
{
    [EventName(nameof(TransferArrivedEvent))]
    public class TransferArrivedEvent : IEvent
    {
        public decimal Amount { get; set; }

        public decimal Balance { get; set; }
    }
}
