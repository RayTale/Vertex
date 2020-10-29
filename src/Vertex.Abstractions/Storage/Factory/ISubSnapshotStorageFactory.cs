using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface ISubSnapshotStorageFactory
    {
        ValueTask<ISubSnapshotStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
