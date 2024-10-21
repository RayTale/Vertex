using Orleans;
using Vertex.Abstractions.Event;

namespace Vertex.Runtime.Test.Events
{
    [EventName(nameof(TransferRefundsEvent))]
    [GenerateSerializer]
    public class TransferRefundsEvent : IEvent
    {
        [Id(0)]
        public decimal Amount { get; set; }

        [Id(1)]
        public decimal Balance { get; set; }
    }
}
