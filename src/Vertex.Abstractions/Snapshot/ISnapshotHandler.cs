using Vertex.Abstractions.Snapshot;
using Vertext.Abstractions.Event;

namespace Vertex.Abstractions.Snapshot
{
    public interface ISnapshotHandler<PrimaryKey, T>: ISnapshotHandlerBase
        where T : ISnapshot
    {
        void Apply(SnapshotUnit<PrimaryKey, T> snapshotBox, EventUnit<PrimaryKey> eventBox);
    }
}
