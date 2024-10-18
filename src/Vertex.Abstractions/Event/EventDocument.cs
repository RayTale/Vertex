using Orleans;

namespace Vertex.Abstractions.Event
{
    [GenerateSerializer]
    public class EventDocument<TPrimaryKey>
    {
        [Id(0)]
        public string FlowId { get; set; }

        [Id(1)]
        public TPrimaryKey ActorId { get; set; }

        [Id(2)]
        public string Name { get; set; }

        [Id(3)]
        public string Data { get; set; }

        [Id(4)]
        public long Timestamp { get; set; }

        [Id(5)]
        public long Version { get; set; }
    }
}