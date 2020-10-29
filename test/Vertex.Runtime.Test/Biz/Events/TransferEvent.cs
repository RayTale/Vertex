using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(TransferEvent))]
    public class TransferEvent : IEvent
    {
        public long ToId { get; set; }

        public decimal Amount { get; set; }

        public decimal Balance { get; set; }
    }
}
