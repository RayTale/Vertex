using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orleans;
using System.Threading.Tasks;
using Transfer.Grains.Events;
using Transfer.Grains.Snapshot;
using Transfer.IGrains.Common;
using Transfer.Repository;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Actor;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertext.Abstractions.Event;

namespace Transfer.Grains.Common
{
    [SnapshotStorage(Consts.CoreDbName, nameof(AccountDb), 3)]
    [StreamSub(nameof(Account), "db", 3)]
    public sealed class AccountDb : FlowActor<long>, IAccountDb
    {
        private readonly IGrainFactory grainFactory;
        private readonly AccountSnapshotHandler accountSnapshotHandler;
        private readonly IMapper mapper;

        public AccountDb(IGrainFactory grainFactory, ISnapshotHandler<long, AccountSnapshot> snapshotHandler)
        {
            this.grainFactory = grainFactory;
            this.accountSnapshotHandler = snapshotHandler as AccountSnapshotHandler;
        }

        public override IVertexActor Vertex => this.grainFactory.GetGrain<IAccount>(this.ActorId);

        public async Task EventHandle(CreateEvent evt, EventMeta eventBase)
        {
            using (var db = new TransferDbContext())
            {
                try
                {
                    var entity = new Repository.Entities.Account()
                    {
                        Id = this.ActorId
                    };
                    
                    this.accountSnapshotHandler.EntityHandle(entity, evt);
                    db.Accounts.Add(entity);
                    await db.SaveChangesAsync();
                }
                catch (System.Exception e)
                {
                    e.PGExceptionHandler<AccountDb>();
                }
            }
        }

        public async Task EventHandle(TransferEvent evt, EventMeta eventBase)
        {
            using (var db = new TransferDbContext())
            {
                var entity = await db.Accounts.FirstOrDefaultAsync(x => x.Id == this.ActorId);
                this.accountSnapshotHandler.EntityHandle(entity, evt);
                await db.SaveChangesAsync();
            }
        }

        public async Task EventHandle(TopupEvent evt, EventMeta eventBase)
        {
            using (var db = new TransferDbContext())
            {
                var entity = await db.Accounts.FirstOrDefaultAsync(x => x.Id == this.ActorId);
                this.accountSnapshotHandler.EntityHandle(entity, evt);
                await db.SaveChangesAsync();
            }
        }

        public async Task EventHandle(TransferArrivedEvent evt, EventMeta eventBase)
        {
            using (var db = new TransferDbContext())
            {
                var entity = await db.Accounts.FirstOrDefaultAsync(x => x.Id == this.ActorId);
                this.accountSnapshotHandler.EntityHandle(entity, evt);
                await db.SaveChangesAsync();
            }
        }

        public async Task EventHandle(TransferRefundsEvent evt, EventMeta eventBase)
        {
            using (var db = new TransferDbContext())
            {
                var entity = await db.Accounts.FirstOrDefaultAsync(x => x.Id == this.ActorId);
                this.accountSnapshotHandler.EntityHandle(entity, evt);
                await db.SaveChangesAsync();
            }
        }
    }
}
