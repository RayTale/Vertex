﻿using Vertex.Abstractions.Event;
using Vertex.Transaction.Abstractions;

namespace Vertex.Transaction.Events.TxUnit
{
    [EventName(nameof(UnitFinishedEvent))]
    public class UnitFinishedEvent : IEvent
    {
        public string TxId { get; set; }

        public TransactionStatus Status { get; set; }
    }
}
