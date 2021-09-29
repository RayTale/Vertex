using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orleans;
using Vertex.Stream.InMemory.IGrains;

namespace Vertex.Stream.InMemory.Grains
{
    public sealed class StreamIdActor : Grain, IStreamIdActor
    {
        private readonly ConcurrentDictionary<string, Guid> container = new ConcurrentDictionary<string, Guid>();

        public Task<Guid> GetId(string topic)
        {
            var id = this.container.GetOrAdd(topic, key => Guid.NewGuid());
            return Task.FromResult(id);
        }
    }
}
