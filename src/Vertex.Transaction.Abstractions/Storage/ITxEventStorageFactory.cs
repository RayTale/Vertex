using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Transaction.Abstractions.Storage
{
    public interface ITxEventStorageFactory
    {
        ValueTask<ITxEventStorage<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor);
    }
}
