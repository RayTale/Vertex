using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Options;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface IInnerTxAccount : IVertexActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();

        Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot();

        ValueTask SetOptions(VertexTxOptions txActorOptions);

        Task BeginTx_Test(string txId = default);

        Task<bool> Commit_Test(string txId = default);

        Task Finish_Test(string txId = default);

        Task Rollbakc_Test(string txId = default);

        Task TopUp(decimal amount, string flowId, string txId = default);
    }
}
