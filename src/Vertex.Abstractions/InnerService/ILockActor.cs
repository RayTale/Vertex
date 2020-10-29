using System.Threading.Tasks;
using Orleans;

namespace Vertex.Abstractions.InnerService
{
    public interface ILockActor : IGrainWithStringKey
    {
        Task<bool> Lock(int millisecondsDelay = 0, int maxMillisecondsHold = 30 * 1000);

        Task Unlock();
    }
}
