using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Transaction.Abstractions.Storage
{
    public interface ITxEventStorageFactory
    {
        ValueTask<ITxEventStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
