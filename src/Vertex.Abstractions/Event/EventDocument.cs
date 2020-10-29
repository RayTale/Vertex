namespace Vertex.Abstractions.Event
{
    public class EventDocument<TPrimaryKey>
    {
        public string FlowId { get; set; }

        public TPrimaryKey ActorId { get; set; }

        public string Name { get; set; }

        public string Data { get; set; }

        public long Timestamp { get; set; }

        public long Version { get; set; }
    }
}
