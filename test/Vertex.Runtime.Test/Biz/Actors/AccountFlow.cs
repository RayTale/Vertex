using Orleans;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Runtime.Actor;
using Vertex.Runtime.Core;
using Vertex.Runtime.Test.Events;
using Vertex.Runtime.Test.IActors;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;

namespace Vertex.Runtime.Test.Actors
{
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(AccountFlow), 3)]
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
