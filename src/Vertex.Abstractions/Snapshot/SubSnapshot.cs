namespace Vertex.Abstractions.Snapshot
{
    public record SubSnapshot<TPrimaryKey>
    {
        public TPrimaryKey ActorId { get; set; }

        public long DoingVersion { get; set; }

        public long Version { get; set; }
    }
}
