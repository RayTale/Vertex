using System.Threading.Tasks;
using Orleans.Concurrency;

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
