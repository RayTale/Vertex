using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Vertex.Transaction.Abstractions
{
    public interface IDTxActor
    {
        [AlwaysInterleave]
        Task<bool> Commit();
        [AlwaysInterleave]
        Task Finish();
        [AlwaysInterleave]
        Task Rollback();
    }
}
