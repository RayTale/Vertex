using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    [EventName(nameof(TxCommitEvent))]
    public class TxCommitEvent : IEvent
    {
        public string Id { get; set; }

        public long StartVersion { get; set; }

        public long StartTime { get; set; }
    }
}
