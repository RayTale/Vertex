using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Abstractions.Storage
{
    public interface ISnapshotStorage<PrimaryKey>
    {
        Task<SnapshotUnit<PrimaryKey, T>> Get<T>(PrimaryKey actorId) where T : ISnapshot, new();

        Task Insert<T>(SnapshotUnit<PrimaryKey, T> snapshot) where T : ISnapshot, new();

        Task Update<T>(SnapshotUnit<PrimaryKey, T> snapshot) where T : ISnapshot, new();

        Task UpdateMinEventTimestamp(PrimaryKey actorId, long timestamp);

        Task UpdateIsLatest(PrimaryKey actorId, bool isLatest);

        Task Delete(PrimaryKey actorId);
    }
}
