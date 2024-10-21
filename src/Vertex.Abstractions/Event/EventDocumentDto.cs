using Orleans;

namespace Vertex.Abstractions.Event
{
    [GenerateSerializer]
    public class EventDocumentDto
    {
        [Id(0)]
        public string Name { get; set; }

        [Id(1)]
        public string Data { get; set; }

        [Id(2)]
        public string FlowId { get; set; }

        [Id(3)]
        public long Timestamp { get; set; }

        [Id(4)]
        public long Version { get; set; }
    }
}
