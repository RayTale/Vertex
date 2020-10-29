using System.Threading.Tasks;
using Orleans;
using Transfer.Grains.Events;
using Transfer.IGrains.Common;
using Vertex.Abstractions.Actor;
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

        public AccountDb(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public override IVertexActor Vertex => this.grainFactory.GetGrain<IAccount>(this.ActorId);

        public Task EventHandle(TransferEvent evt, EventMeta eventBase)
        {
            // Update database here
            return Task.CompletedTask;
        }

        public Task EventHandle(TopupEvent evt, EventMeta eventBase)
        {
            // Update database here
            return Task.CompletedTask;
        }

        public Task EventHandle(TransferArrivedEvent evt, EventMeta eventBase)
        {
            // Update database here
            return Task.CompletedTask;
        }

        public Task EventHandle(TransferRefundsEvent evt, EventMeta eventBase)
        {
            // Update database here
            return Task.CompletedTask;
        }
    }
}
