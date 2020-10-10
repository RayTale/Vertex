using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.EventStream
{
    public interface IEventStreamFactory
    {
        ValueTask<IEventStream> Create<PrimaryKey>(IActor<PrimaryKey> actor);
    }
}
