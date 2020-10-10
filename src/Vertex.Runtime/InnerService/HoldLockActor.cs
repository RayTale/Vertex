using System;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.InnerService;

namespace Vertex.Runtime.InnerService
{
    public class HoldLockActor : Grain, IHoldLockActor
    {
        private long lockId;
        private long expireTime;

        public Task<(bool isOk, long lockId)> Lock(int holdingSeconds = 30)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (this.lockId == 0 || now > this.expireTime)
            {
                this.lockId = now;
                this.expireTime = now + holdingSeconds * 1000;
                return Task.FromResult((true, now));
            }
            else
            {
                return Task.FromResult((false, (long)0));
            }
        }

        public Task<bool> Hold(long lockId, int holdingSeconds = 30)
        {
            if (this.lockId == lockId)
            {
                this.expireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + holdingSeconds * 1000;
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task Unlock(long lockId)
        {
            if (this.lockId == lockId)
            {
                this.lockId = 0;
                this.expireTime = 0;
            }

            return Task.CompletedTask;
        }
    }
}
