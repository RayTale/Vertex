using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Abstractions.Storage
{
    public interface ISnapshotStorage<TPrimaryKey>
    {
        Task<SnapshotUnit<TPrimaryKey, T>> Get<T>(TPrimaryKey actorId)
            where T : ISnapshot, new();

        Task Insert<T>(SnapshotUnit<TPrimaryKey, T> snapshot)
            where T : ISnapshot, new();

        Task Update<T>(SnapshotUnit<TPrimaryKey, T> snapshot)
            where T : ISnapshot, new();

        Task UpdateMinEventTimestamp(TPrimaryKey actorId, long timestamp);

        Task UpdateIsLatest(TPrimaryKey actorId, bool isLatest);

        Task Delete(TPrimaryKey actorId);
    }
}
