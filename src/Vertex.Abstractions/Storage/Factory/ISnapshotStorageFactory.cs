using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface ISnapshotStorageFactory
    {
        ValueTask<ISnapshotStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
