using Orleans;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(TransferEvent))]
    [GenerateSerializer]
    public class TransferEvent : IEvent
    {
        [Id(0)]
        public long ToId { get; set; }

        [Id(1)]
        public decimal Amount { get; set; }

        [Id(2)]
        public decimal Balance { get; set; }
    }
}
