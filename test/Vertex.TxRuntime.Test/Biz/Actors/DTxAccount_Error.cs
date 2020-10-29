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
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), 3)]
    [TxEventStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount))]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(DTxAccount), 3)]
    [Stream(nameof(ReentryAccount), 3)]
    public class DTxAccount_Error : DTxActor<long, AccountSnapshot>, IDTxAccount_Error
    {
        public Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot()
        {
            return Task.FromResult(this.Snapshot);
        }

        public Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot()
        {
            return Task.FromResult(this.BackupSnapshot);
        }

        /// <summary>
        /// for test
        /// </summary>
        /// <param name="txId">transaction id</param>
        /// <returns></returns>
        protected override ValueTask OnTxCommit(string txId)
        {
            throw new Exception(nameof(this.OnTxCommit));
        }

        public async Task<bool> Transfer(long toAccountId, decimal amount)
        {
            if (this.Snapshot.Data.Balance >= amount)
            {
                var evt = new TransferEvent
                {
                    Amount = amount,
                    Balance = this.Snapshot.Data.Balance - amount,
                    ToId = toAccountId
                };
                await this.TxRaiseEvent(evt);
                return true;
            }
            else
            {
                return false;
            }
        }

        public Task TransferArrived(decimal amount)
        {
            var evt = new TransferArrivedEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            return this.TxRaiseEvent(evt).AsTask();
        }
    }
}
