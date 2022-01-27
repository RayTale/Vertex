using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    [EventName(nameof(TxCommitedEvent))]
    public class TxCommitedEvent : IEvent
    {
        public string Id { get; set; }
    }
}
