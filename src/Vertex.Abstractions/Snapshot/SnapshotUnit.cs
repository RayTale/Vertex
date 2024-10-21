using Orleans;

namespace Vertex.Abstractions.Snapshot
{
    [GenerateSerializer]
    public record SnapshotUnit<TPrimaryKey, T>
        where T : ISnapshot
    {
        [Id(0)]
        public SnapshotMeta<TPrimaryKey> Meta { get; set; }

        [Id(1)]
        public T Data { get; set; }
    }
}
