using Orleans;
using System.Threading.Tasks;
using Transfer.Grains.Events;
using Transfer.IGrains.Common;
using Vertex.Abstractions.Actor;
using Vertex.Runtime.Actor;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;

namespace Transfer.Grains.Common
{
    [SnapshotStorage(Consts.core_db_Name, nameof(AccountFlow), 3)]
    [StreamSub(nameof(Account), "flow", 3)]

    public sealed class AccountFlow : FlowActor<long>, IAccountFlow
    {
        readonly IGrainFactory grainFactory;
        public AccountFlow(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }
        public override IVertexActor Vertex => grainFactory.GetGrain<IAccount>(this.ActorId);
        public Task EventHandle(TransferEvent evt)
        {
            var toActor = GrainFactory.GetGrain<IAccount>(evt.ToId);
            return toActor.TransferArrived(evt.Amount);
        }
    }
}
