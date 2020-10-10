using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface ISnapshotStorageFactory
    {
        ValueTask<ISnapshotStorage<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor);
    }
}
