using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Transfer.Grains.Events;
using Transfer.Grains.Snapshot;
using Transfer.IGrains.Common;
using Transfer.Repository;
using Vertex.Abstractions.Snapshot;
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
        protected override async ValueTask CreateSnapshot()
        {
            await base.CreateSnapshot();
            using (var db = new TransferDbContext())
            {
                var entity = await db.Accounts.FirstOrDefaultAsync(x => x.Id == this.ActorId);
                if (entity != null)
                {
                    this.Snapshot.Data = this.ServiceProvider.GetService<IMapper>().Map<Repository.Entities.Account, AccountSnapshot>(entity);
                }
            }
        }

        public Task<bool> Create(decimal amount, string createId)
        {
            if (this.ActivateSnapshotVersion > 0 || this.Snapshot.Data.Balance != default)
            {
                return Task.FromResult(false);
            }

            var evt = new CreateEvent
            {
                Balance = amount
            };
            return this.RaiseEvent(evt, createId);
        }

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
