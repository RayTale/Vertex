namespace Vertex.Abstractions.Snapshot
{
    public record SnapshotUnit<TPrimaryKey, T>
        where T : ISnapshot
    {
        public SnapshotMeta<TPrimaryKey> Meta { get; set; }

        public T Data { get; set; }
    }
}
