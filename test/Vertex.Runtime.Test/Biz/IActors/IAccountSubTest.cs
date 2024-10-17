using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Runtime.Test.IActors
{
    public interface IAccountSubTest : IFlowActor, IGrainWithIntegerKey
    {
        ValueTask<bool> IsConcurrentHandle();

        ValueTask<SubSnapshot<long>> GetSnapshot();

        ValueTask Tell_Test(EventUnit<long> eventUnit);

        Task Tell_Test(List<EventUnit<long>> list);

        Task ConcurrentTell_Test(List<EventUnit<long>> list);

        ValueTask<int> GetExecutedTimes();

        ValueTask SetConcurrentHandle(bool value);
    }
}
