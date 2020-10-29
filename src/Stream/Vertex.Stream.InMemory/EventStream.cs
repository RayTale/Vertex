using System.Threading.Tasks;
using Orleans.Streams;
using Vertex.Abstractions.EventStream;

namespace Vertex.Stream.InMemory
{
    public class EventStream : IEventStream
    {
        private readonly IAsyncStream<byte[]> asyncStream;

        public EventStream(IAsyncStream<byte[]> asyncStream)
        {
            this.asyncStream = asyncStream;
        }

        public ValueTask Next(byte[] bytes)
        {
            return new ValueTask(this.asyncStream.OnNextAsync(bytes));
        }
    }
}
