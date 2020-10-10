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
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(ReentryDTxAccount), 3)]
    [TxEventStorage(TestSiloConfigurations.TestConnectionName, nameof(ReentryDTxAccount))]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(ReentryDTxAccount), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(ReentryDTxAccount), 3)]
    [Stream(nameof(ReentryAccount), 3)]
    public class ReentryDTxAccount : ReentryDTxActor<long, AccountSnapshot>, IReentryDTxAccount
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

        public Task TopUp(decimal amount, string flowId)
        {
            return ConcurrentRaiseEvent(async (snapshot, func) =>
            {
                var evt = new TopupEvent
                {
                    Amount = amount,
                    Balance = Snapshot.Data.Balance + amount
                };
                await func(evt);
            }, flowId);
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
    }
}
