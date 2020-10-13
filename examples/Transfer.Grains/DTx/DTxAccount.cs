using System.Threading.Tasks;
using Transfer.Grains.Events;
using Transfer.Grains.Snapshot;
using Transfer.IGrains.DTx;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Actor;

namespace Transfer.Grains.DTx
{
    [TxEventStorage(Consts.core_db_Name, nameof(DTxAccount))]
    [EventStorage(Consts.core_db_Name, nameof(DTxAccount), 3)]
    [EventArchive(Consts.core_db_Name, nameof(DTxAccount), "month")]
    [SnapshotStorage(Consts.core_db_Name, nameof(DTxAccount), 3)]
    [Stream(nameof(DTxAccount), 3)]
    public class DTxAccount : DTxActor<long, AccountSnapshot>, IDTxAccount
    {
        public Task<decimal> GetBalance()
        {
            return Task.FromResult(Snapshot.Data.Balance);
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
