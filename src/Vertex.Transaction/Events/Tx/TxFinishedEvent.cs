using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    [EventName(nameof(TxFinishedEvent))]
    public class TxFinishedEvent : IEvent
    {
        public string Id { get; set; }
    }
}
