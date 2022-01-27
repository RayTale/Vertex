using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Snapshot
{
    public interface ISnapshotHandler<TPrimaryKey, T> : ISnapshotHandlerBase
        where T : ISnapshot
    {
        void Apply(SnapshotUnit<TPrimaryKey, T> snapshotBox, EventUnit<TPrimaryKey> eventBox);
    }
}
