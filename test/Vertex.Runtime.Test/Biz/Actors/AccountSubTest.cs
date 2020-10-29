using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Actor;
using Vertex.Runtime.Actor.Attributes;
using Vertex.Runtime.Core;
using Vertex.Runtime.Test.Events;
using Vertex.Runtime.Test.IActors;
using Vertex.Storage.Linq2db.Core;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Test.Biz.Actors
{
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(AccountSubTest), 3)]
    [EventReentrant]
    public class AccountSubTest : FlowActor<long>, IAccountSubTest
    {
        private readonly IGrainFactory grainFactory;
        private int executedTimes;

        public AccountSubTest(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public override IVertexActor Vertex => this.grainFactory.GetGrain<IAccount>(this.ActorId);

        public ValueTask<SubSnapshot<long>> GetSnapshot()
        {
            return ValueTask.FromResult(this.Snapshot);
        }

        public ValueTask<int> GetExecutedTimes()
        {
            return ValueTask.FromResult(this.executedTimes);
        }

        public ValueTask Tell_Test(EventUnit<long> eventUnit)
        {
            return this.Tell(eventUnit);
        }

        public async Task Tell_Test(List<EventUnit<long>> list)
        {
            foreach (var item in list)
            {
                await this.Tell(item);
            }
        }

        public Task ConcurrentTell_Test(List<EventUnit<long>> list)
        {
            return this.ConcurrentTell(list);
        }

        public Task EventHandle(TopupEvent evt, EventMeta eventBase)
        {
            Interlocked.Increment(ref this.executedTimes);

            // Update database here
            return Task.CompletedTask;
        }

        public ValueTask<bool> IsConcurrentHandle()
        {
            return ValueTask.FromResult(this.ConcurrentHandle);
        }

        public ValueTask SetConcurrentHandle(bool value)
        {
            this.ConcurrentHandle = value;
            return ValueTask.CompletedTask;
        }
    }
}
