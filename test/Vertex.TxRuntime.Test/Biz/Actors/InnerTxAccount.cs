using Orleans.Concurrency;
using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Actor;
using Vertex.Transaction.Options;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Vertex.TxRuntime.Test.Events;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.Actors
{
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(InnerTxAccount), 3)]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(InnerTxAccount), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(InnerTxAccount), 3)]
    [Stream(nameof(InnerTxAccount), 3)]
    [Reentrant]
    public class InnerTxAccount : InnerTxActor<long, AccountSnapshot>, IInnerTxAccount
    {
        public Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot()
        {
            return Task.FromResult(this.Snapshot);
        }
        public Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot()
        {
            return Task.FromResult(this.BackupSnapshot);
        }
        public ValueTask SetOptions(VertexTxOptions txActorOptions)
        {
            this.VertexTxOptions.TxSecondsTimeout = txActorOptions.TxSecondsTimeout;
            return ValueTask.CompletedTask;
        }
        public Task BeginTx_Test(string txId = default)
        {
            return this.BeginTransaction(txId);
        }
        public Task<bool> Commit_Test(string txId = default)
        {
            return this.Commit(txId);
        }
        public Task Finish_Test(string txId = default)
        {
            return this.Finish(txId);
        }
        public Task Rollbakc_Test(string txId = default)
        {
            return this.Rollback(txId);
        }

        public async Task TopUp(decimal amount, string flowId, string txId = default)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = Snapshot.Data.Balance + amount
            };
            await TxRaiseEvent(evt, flowId, txId);
        }
    }
}
