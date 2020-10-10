using Vertex.Abstractions.Event;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    [EventName(nameof(TxRollbackEvent))]
    public class TxRollbackEvent : IEvent
    {
        public string Id { get; set; }
    }
}
