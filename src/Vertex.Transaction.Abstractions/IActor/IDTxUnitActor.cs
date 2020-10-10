using Orleans.Concurrency;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Transaction.Abstractions.IActor
{
    public interface IDTxUnitActor<TRequest, TResponse> : IVertexActor
    {
        [AlwaysInterleave]
        Task<TResponse> Ask(TRequest request);
    }
}
