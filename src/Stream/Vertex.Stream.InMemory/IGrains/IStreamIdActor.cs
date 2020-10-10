using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Vertex.Stream.InMemory.IGrains
{
    public interface IStreamIdActor: IGrainWithIntegerKey
    {
        [AlwaysInterleave]
        Task<Guid> GetId(string topic);
    }
}
