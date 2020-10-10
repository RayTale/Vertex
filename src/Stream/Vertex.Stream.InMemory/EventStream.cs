using Orleans.Streams;
using System.Threading.Tasks;
using Vertex.Abstractions.EventStream;

namespace Vertex.Stream.InMemory
{
    public class EventStream : IEventStream
    {
        readonly IAsyncStream<byte[]> asyncStream;
        public EventStream(IAsyncStream<byte[]> asyncStream)
        {
            this.asyncStream = asyncStream;
        }
        public ValueTask Next(byte[] bytes)
        {
            return new ValueTask(asyncStream.OnNextAsync(bytes));
        }
    }
}
