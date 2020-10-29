namespace Vertex.Abstractions.Event
{
    public class EventDocumentDto
    {
        public string Name { get; set; }

        public string Data { get; set; }

        public string FlowId { get; set; }

        public long Timestamp { get; set; }

        public long Version { get; set; }
    }
}
