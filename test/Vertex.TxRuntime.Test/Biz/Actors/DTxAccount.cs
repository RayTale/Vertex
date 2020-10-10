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
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), 3)]
    [TxEventStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount))]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), 3)]
    [Stream(nameof(ReentryAccount), 3)]
    public class DTxAccount : DTxActor<long, AccountSnapshot>, IDTxAccount
    {
        public Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot()
        {
            return Task.FromResult(this.Snapshot);
        }
        public Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot()
        {
            return Task.FromResult(this.BackupSnapshot);
        }

        public ValueTask SetOptions(VertexDtxOptions txActorOptions)
        {
            this.DtxOptions.RetainedTxEvents = txActorOptions.RetainedTxEvents;
            return ValueTask.CompletedTask;
        }

        public async Task TopUp(decimal amount, string flowId)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = Snapshot.Data.Balance + amount
            };
            await RaiseEvent(evt, flowId);
        }
        public Task<bool> Commit_Test()
        {
            return this.Commit();
        }
        public Task Finish_Test()
        {
            return this.Finish();
        }
        public Task Rollbakc_Test()
        {
            return this.Rollback();
        }

        public async Task<bool> Transfer(long toAccountId, decimal amount)
        {
            if (Snapshot.Data.Balance >= amount)
            {
                var evt = new TransferEvent
                {
                    Amount = amount,
                    Balance = Snapshot.Data.Balance - amount,
                    ToId = toAccountId
                };
                await TxRaiseEvent(evt);
                return true;
            }
            else
                return false;
        }
        public Task TransferArrived(decimal amount)
        {
            var evt = new TransferArrivedEvent
            {
                Amount = amount,
                Balance = Snapshot.Data.Balance + amount
            };
            return TxRaiseEvent(evt).AsTask();
        }
    }
}
