using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Vertex.Stream.InMemory.IGrains
{
    public interface IStreamIdActor : IGrainWithIntegerKey
    {
        [AlwaysInterleave]
        Task<Guid> GetId(string topic);
    }
}
