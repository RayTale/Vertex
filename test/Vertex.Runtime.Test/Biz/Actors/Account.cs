﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Actor;
using Vertex.Runtime.Core;
using Vertex.Runtime.Options;
using Vertex.Runtime.Test.Events;
using Vertex.Runtime.Test.IActors;
using Vertex.Runtime.Test.Snapshot;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;

namespace Vertex.Runtime.Test.Actors
{
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(Account), 3)]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(Account), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(Account), 3)]
    [Stream(nameof(Account), 3)]
    public sealed class Account : VertexActor<long, AccountSnapshot>, IAccount
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

        public Task<bool> Transfer(long toAccountId, decimal amount, string transferId)
        {
            if (this.Snapshot.Data.Balance >= amount)
            {
                var evt = new TransferEvent
                {
                    Amount = amount,
                    Balance = this.Snapshot.Data.Balance - amount,
                    ToId = toAccountId,
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
                Balance = this.Snapshot.Data.Balance + amount,
            };
            return this.RaiseEvent(evt, topupId);
        }

        public Task<bool> TopUp(decimal amount)
        {
            var evt = new TopupEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount,
            };
            return this.RaiseEvent(evt);
        }

        public Task TransferArrived(decimal amount)
        {
            var evt = new TransferArrivedEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount,
            };
            return this.RaiseEvent(evt);
        }

        public Task<bool> TransferRefunds(decimal amount)
        {
            var evt = new TransferRefundsEvent
            {
                Amount = amount,
                Balance = this.Snapshot.Data.Balance + amount,
            };
            return this.RaiseEvent(evt);
        }

        public async Task RecoverySnapshot_Test()
        {
            await this.RecoverySnapshot();
        }

        public async Task Deactivate_Test()
        {
            await this.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
            await this.CreateSnapshot();
        }

        public Task<(bool can, long endTimestamp)> CanArchive_Test()
        {
            return Task.FromResult(this.CanArchive());
        }

        public Task Archive_Test(long endTimestamp)
        {
            return this.Archive(endTimestamp);
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
                Timestamp = o.Timestamp,
            }).ToList();
        }

        public async Task<int> GetArchiveEventCount()
        {
            var list = await this.EventArchive.GetList(this.ActorId, 0, this.Snapshot.Meta.Version);
            return list.Count;
        }

        public Task HandlerError_Test()
        {
            return this.RaiseEvent(new ErrorTestEvent(), Guid.NewGuid().ToString());
        }
    }
}
