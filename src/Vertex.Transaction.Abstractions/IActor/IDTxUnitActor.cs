using System.Threading.Tasks;
using Orleans.Concurrency;
using Vertex.Abstractions.Actor;

namespace Vertex.Transaction.Abstractions.IActor
{
    public interface IDTxUnitActor<TRequest, TResponse> : IVertexActor
    {
        [AlwaysInterleave]
        Task<TResponse> Ask(TRequest request);
    }
}
