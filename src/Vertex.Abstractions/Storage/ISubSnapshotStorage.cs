using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Abstractions.Storage
{
    public interface ISubSnapshotStorage<PrimaryKey>
    {
        Task<SubSnapshot<PrimaryKey>> Get(PrimaryKey id);

        Task Insert(SubSnapshot<PrimaryKey> snapshot);

        Task Update(SubSnapshot<PrimaryKey> snapshot);

        Task Delete(PrimaryKey id);
    }
}
