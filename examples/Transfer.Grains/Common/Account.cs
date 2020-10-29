using System.Threading.Tasks;
using Transfer.Grains.Events;
using Transfer.Grains.Snapshot;
using Transfer.IGrains.Common;
using Vertex.Runtime.Actor;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;

namespace Transfer.Grains.Common
{
    [EventStorage(Consts.CoreDbName, nameof(Account), 3)]
    [EventArchive(Consts.CoreDbName, nameof(Account), "month")]
    [SnapshotStorage(Consts.CoreDbName, nameof(Account), 3)]
    [Stream(nameof(Account), 3)]
    public sealed class Account : VertexActor<long, AccountSnapshot>, IAccount
    {
        public Task<decimal> GetBalance()
        {
            return Task.FromResult(this.Snapshot.Data.Balance);
        }

        public Task<bool> Transfer(long toAccountId, decimal amount, string transferId)
        {
            if (this.Snapshot.Data.Balance >= amount)
            {
                var evt = new TransferEvent
                {
                    Amount = amount,
                    Balance = this.Snapshot.Data.Balance - amount,
                    ToId = toAccountId
                };
                return this.RaiseEvent(evt, transferId);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> TopUp(decimal amount, string topupId)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            return this.RaiseEvent(evt, topupId);
        }

        public Task TransferArrived(decimal amount)
        {
            var evt = new TransferArrivedEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            return this.RaiseEvent(evt);
        }

        public Task<bool> TransferRefunds(decimal amount)
        {
            var evt = new TransferRefundsEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            return this.RaiseEvent(evt);
        }
    }
}
