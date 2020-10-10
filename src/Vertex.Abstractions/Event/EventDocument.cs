namespace Vertex.Abstractions.Event
{
    public class EventDocument<PrimaryKey>
    {
        public string FlowId { get; set; }
        public PrimaryKey ActorId { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public long Timestamp { get; set; }
        public long Version { get; set; }
    }
}
