using System.Threading.Tasks;

namespace Vertex.Abstractions.EventStream
{
    public interface IEventStream
    {
        ValueTask Next(byte[] bytes);
    }
}
