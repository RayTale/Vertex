using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Vertex.Abstractions.EventStream
{
    public interface IStreamHandler
    {
        Task OnNext(Immutable<byte[]> bytes);

        Task OnNext(Immutable<List<byte[]>> items);
    }
}
