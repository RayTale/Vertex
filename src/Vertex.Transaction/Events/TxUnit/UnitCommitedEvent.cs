using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events.TxUnit
{
    [EventName(nameof(UnitCommitedEvent))]
    public class UnitCommitedEvent : IEvent
    {
        public string TxId { get; set; }
    }
}
