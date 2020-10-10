namespace Vertex.Abstractions.Snapshot
{
    public record SnapshotUnit<PrimaryKey, T>
        where T : ISnapshot
    {
        public SnapshotMeta<PrimaryKey> Meta { get; set; }

        public T Data { get; set; }
    }
}
