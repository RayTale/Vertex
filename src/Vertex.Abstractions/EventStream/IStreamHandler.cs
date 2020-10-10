using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vertex.Abstractions.EventStream
{
    public interface IStreamHandler
    {
        Task OnNext(Immutable<byte[]> bytes);

        Task OnNext(Immutable<List<byte[]>> items);
    }
}
