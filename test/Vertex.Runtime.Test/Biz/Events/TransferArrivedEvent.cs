using Orleans;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(TransferArrivedEvent))]
    [GenerateSerializer]
    public class TransferArrivedEvent : IEvent
    {
        [Id(0)]
        public decimal Amount { get; set; }

        [Id(1)]
        public decimal Balance { get; set; }
    }
}
