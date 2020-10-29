using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface IEventArchiveFactory
    {
        ValueTask<IEventArchive<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
