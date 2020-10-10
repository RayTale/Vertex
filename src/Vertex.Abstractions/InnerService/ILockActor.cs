using Orleans;
using System.Threading.Tasks;

namespace Vertex.Abstractions.InnerService
{
    public interface ILockActor : IGrainWithStringKey
    {
        Task<bool> Lock(int millisecondsDelay = 0);
        Task Unlock();
    }
}
