using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface IEventStorageFactory
    {
        ValueTask<IEventStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
