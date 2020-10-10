using System;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.InnerService;

namespace Vertex.Runtime.InnerService
{
    public class WeightHoldLockActor : Grain, IWeightHoldLockActor
    {
        private long lockId;
        private long expireTime;
        private int currentWeight;
        private int maxWaitWeight = -1;

        public Task<bool> Hold(long lockId, int holdingSeconds = 30)
        {
            if (this.lockId == lockId && this.currentWeight >= this.maxWaitWeight)
            {
                this.expireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + holdingSeconds * 1000;
                return Task.FromResult(true);
            }
            else
            {
                this.Unlock(lockId);
                return Task.FromResult(false);
            }
        }

        public Task<(bool isOk, long lockId, int expectMillisecondDelay)> Lock(int weight, int holdingSeconds = 30)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if ((this.lockId == 0 || now > this.expireTime) && weight >= maxWaitWeight)
            {
                this.lockId = now;
                this.currentWeight = weight;
                this.maxWaitWeight = -1;
                this.expireTime = now + holdingSeconds * 1000;
                return Task.FromResult((true, now, 0));
            }

            if (weight >= this.maxWaitWeight && weight > this.currentWeight)
            {
                this.maxWaitWeight = weight;
                return Task.FromResult((false, (long)0, (int)(this.expireTime - now)));
            }

            return Task.FromResult((false, (long)0, 0));
        }

        public Task Unlock(long lockId)
        {
            if (this.lockId == lockId)
            {
                this.lockId = 0;
                this.currentWeight = 0;
                this.expireTime = 0;
            }

            return Task.CompletedTask;
        }
    }
}
