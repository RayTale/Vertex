using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Abstractions.EventStream
{
    public interface IEventStreamFactory
    {
        ValueTask<IEventStream> Create<TPrimaryKey>(IActor<TPrimaryKey> actor);
    }
}
