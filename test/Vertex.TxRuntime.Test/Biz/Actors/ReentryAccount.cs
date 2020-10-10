using Orleans.Concurrency;
using System;
using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Actor;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Vertex.TxRuntime.Test.Events;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.Actors
{
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(ReentryAccount), 3)]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(ReentryAccount), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(ReentryAccount), 3)]
    [Stream(nameof(ReentryAccount), 3)]
    [Reentrant]
    public class ReentryAccount : ReentryActor<long, AccountSnapshot>, IReentryAccount
    {
        public Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot()
        {
            return Task.FromResult(this.Snapshot);
        }
        public Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot()
        {
            return Task.FromResult(this.BackupSnapshot);
        }
        public Task<bool> TopUp(decimal amount, string flowId)
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
        public Task ErrorTest()
        {
            return ConcurrentRaiseEvent(async (snapshot, func) =>
            {
                await func(new ErrorTestEvent());
            }, Guid.NewGuid().ToString());
        }
    }
}
