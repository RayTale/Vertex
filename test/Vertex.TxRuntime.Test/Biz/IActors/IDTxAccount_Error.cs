using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface IDTxAccount_Error : IVertexActor, IDTxActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();

        Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot();

        Task<bool> Transfer(long toAccountId, decimal amount);

        Task TransferArrived(decimal amount);
    }
}
