using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Options;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface IReentryDTxAccount : IVertexActor, IDTxActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();

        Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot();

        ValueTask SetOptions(VertexDtxOptions txActorOptions);

        Task TopUp(decimal amount, string flowId);

        Task<bool> Commit_Test();

        Task Finish_Test();

        Task Rollbakc_Test();
    }
}
