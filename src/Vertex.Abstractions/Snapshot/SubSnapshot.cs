namespace Vertex.Abstractions.Snapshot
{
    public record SubSnapshot<PrimaryKey>
    {
        public PrimaryKey ActorId { get; set; }

        public long DoingVersion { get; set; }

        public long Version { get; set; }
    }
}
