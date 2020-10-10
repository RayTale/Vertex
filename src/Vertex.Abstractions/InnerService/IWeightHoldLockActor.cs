using System.Threading.Tasks;
using Orleans;

namespace Vertex.Abstractions.InnerService
{
    public interface IWeightHoldLockActor : IGrainWithStringKey
    {
        Task<(bool isOk, long lockId, int expectMillisecondDelay)> Lock(int weight, int holdingSeconds = 30);

        Task<bool> Hold(long lockId, int holdingSeconds = 30);

        Task Unlock(long lockId);
    }
}
