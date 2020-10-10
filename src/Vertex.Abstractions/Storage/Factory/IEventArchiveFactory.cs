using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.Storage
{
    public interface IEventArchiveFactory
    {
        ValueTask<IEventArchive<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor);
    }
}
