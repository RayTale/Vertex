using System;
using System.Data;
using System.Text;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Serialization;
using Vertex.Protocol;
using Vertex.Runtime.Event;
using Vertex.Utils;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    public class EventBufferUnit<TPrimaryKey> : IDisposable
    {
        private readonly string eventName;
        private readonly SharedArray eventBaseArray;
        private readonly SharedArray eventTransArray;

        public EventBufferUnit(EventUnit<TPrimaryKey> eventUnit, IEventTypeContainer eventTypeContainer, ISerializer serializer)
            : this(eventUnit, default, eventTypeContainer, serializer)
        {
        }

        public EventBufferUnit(EventUnit<TPrimaryKey> eventUnit, string flowId, IEventTypeContainer eventTypeContainer, ISerializer serializer)
        {
            var evtType = eventUnit.Event.GetType();
            if (!eventTypeContainer.TryGet(evtType, out this.eventName))
            {
                throw new NoNullAllowedException($"event name of {evtType.FullName}");
            }

            this.eventBaseArray = eventUnit.Meta.ConvertToBytes();
            this.EventBytes = serializer.SerializeToUtf8Bytes(eventUnit.Event, evtType);
            this.eventTransArray = EventConverter.ConvertToBytes(new EventTransUnit(this.eventName, eventUnit.ActorId, this.eventBaseArray.AsSpan(), this.EventBytes));
            this.EventUnit = eventUnit;
            this.Document = new EventDocument<TPrimaryKey>
            {
                ActorId = eventUnit.ActorId,
                Data = Encoding.UTF8.GetString(this.EventBytes),
                FlowId = flowId,
                Name = this.eventName,
                Version = eventUnit.Meta.Version,
                Timestamp = eventUnit.Meta.Timestamp
            };
        }

        public byte[] EventBytes { get; }

        public EventUnit<TPrimaryKey> EventUnit { get; set; }

        public EventDocument<TPrimaryKey> Document { get; }

        public Span<byte> GetEventTransSpan() => this.eventTransArray.AsSpan();

        public void Dispose()
        {
            this.eventBaseArray.Dispose();
            this.eventTransArray.Dispose();
        }
    }
}
