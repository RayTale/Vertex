using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events.TxUnit
{
    [EventName(nameof(UnitRollbackEvent))]
    public class UnitRollbackEvent : IEvent
    {
        public string TxId { get; set; }
    }
}
