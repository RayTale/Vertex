using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface ISubSnapshotStorageFactory
    {
        ValueTask<ISubSnapshotStorage<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor);
    }
}
