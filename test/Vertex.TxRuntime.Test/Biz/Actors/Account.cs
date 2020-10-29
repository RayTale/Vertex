using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Options;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Actor;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Vertex.TxRuntime.Test.Events;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.Runtime.Test.Actors
{
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(Account), 3)]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(Account), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(Account), 3)]
    [Stream(nameof(Account), 3)]
    public sealed class Account : ReentryDTxActor<long, AccountSnapshot>, IAccount
    {
        public Task<decimal> GetBalance()
        {
            return Task.FromResult(this.Snapshot.Data.Balance);
        }

        public Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot()
        {
            return Task.FromResult(this.Snapshot);
        }

        public Task<long> GetActivateSnapshotVersion()
        {
            return Task.FromResult(this.ActivateSnapshotVersion);
        }

        public Task<ActorOptions> GetOptions()
        {
            return Task.FromResult(this.VertexOptions);
        }

        public Task<ArchiveOptions> GetArchiveOptions()
        {
            return Task.FromResult(this.ArchiveOptions);
        }

        public Task SetArchiveOptions(ArchiveOptions archiveOptions)
        {
            this.ArchiveOptions.MinIntervalSeconds = archiveOptions.MinIntervalSeconds;
            this.ArchiveOptions.EventPageSize = archiveOptions.EventPageSize;
            this.ArchiveOptions.RetainSeconds = archiveOptions.RetainSeconds;
            return Task.CompletedTask;
        }

        public Task<bool> Transfer(long toAccountId, decimal amount)
        {
            if (this.Snapshot.Data.Balance >= amount)
            {
                var evt = new TransferEvent
                {
                    Amount = amount,
                    Balance = this.Snapshot.Data.Balance - amount,
                    ToId = toAccountId
                };
                return this.RaiseEvent(evt);
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

        public Task<bool> TopUp(decimal amount)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount
            };
            return this.RaiseEvent(evt);
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

        public async Task RecoverySnapshot_Test()
        {
            await this.RecoverySnapshot();
        }

        public async Task<List<EventDocumentDto>> GetEventDocuments_FromEventStorage(long startVersion, long endVersion)
        {
            var results = await this.EventStorage.GetList(this.ActorId, startVersion, endVersion);
            return results.Select(o => new EventDocumentDto
            {
                Data = o.Data,
                Name = o.Name,
                FlowId = o.FlowId,
                Version = o.Version,
                Timestamp = o.Timestamp
            }).ToList();
        }

        public Task HandlerError_Test()
        {
            return this.RaiseEvent(new ErrorTestEvent(), Guid.NewGuid().ToString());
        }
    }
}
