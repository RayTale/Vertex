namespace Vertex.Abstractions.Snapshot
{
    public record SnapshotMeta<PrimaryKey>
    {
        public PrimaryKey ActorId { get; set; }

        public long DoingVersion { get; set; }

        public long Version { get; set; }
        public long MinEventTimestamp { get; set; }
        public long MinEventVersion { get; set; }

        public bool IsLatest { get; set; }
    }
}
