using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Events.TxUnit
{
    [EventName(nameof(UnitCommitEvent))]
    public class UnitCommitEvent : IEvent
    {
        public string TxId { get; set; }

        public string Data { get; set; }

        public long StartTime { get; set; }
    }
}
