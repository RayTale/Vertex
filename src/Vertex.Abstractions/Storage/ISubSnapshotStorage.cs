using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Abstractions.Storage
{
    public interface ISubSnapshotStorage<TPrimaryKey>
    {
        Task<SubSnapshot<TPrimaryKey>> Get(TPrimaryKey id);

        Task Insert(SubSnapshot<TPrimaryKey> snapshot);

        Task Update(SubSnapshot<TPrimaryKey> snapshot);

        Task Delete(TPrimaryKey id);
    }
}
