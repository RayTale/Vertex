using Orleans;
using Vertex.Abstractions.Actor;

namespace Vertex.Runtime.Test.IActors
{
    public interface IAccountDb : IFlowActor, IGrainWithIntegerKey
    {
    }
}
