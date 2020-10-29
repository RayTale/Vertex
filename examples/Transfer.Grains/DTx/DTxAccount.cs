using System.Threading.Tasks;
using Transfer.Grains.Events;
using Transfer.Grains.Snapshot;
using Transfer.IGrains.DTx;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Actor;

namespace Transfer.Grains.DTx
{
    [TxEventStorage(Consts.CoreDbName, nameof(DTxAccount))]
    [EventStorage(Consts.CoreDbName, nameof(DTxAccount), 3)]
    [EventArchive(Consts.CoreDbName, nameof(DTxAccount), "month")]
    [SnapshotStorage(Consts.CoreDbName, nameof(DTxAccount), 3)]
    [Stream(nameof(DTxAccount), 3)]
    public class DTxAccount : DTxActor<long, AccountSnapshot>, IDTxAccount
    {
        public Task<decimal> GetBalance()
        {
            return Task.FromResult(this.Snapshot.Data.Balance);
        }

        public async Task TopUp(decimal amount, string flowId)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            await this.RaiseEvent(evt, flowId);
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
