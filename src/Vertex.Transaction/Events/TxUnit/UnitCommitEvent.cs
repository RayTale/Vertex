using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events.TxUnit
{
    [EventName(nameof(UnitCommitEvent))]
    public class UnitCommitEvent : IEvent
    {
        public string TxId { get; set; }

        public byte[] Data { get; set; }

        public long StartTime { get; set; }
    }
}
