using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Vertex.Abstractions.InnerService;
using Vertex.Runtime.Core;
using Xunit;

namespace Vertex.Runtime.Test.InnerService
{
    [Collection(ClusterCollection.Name)]
    public class HoldLockActor_Test
    {
        private readonly TestCluster _cluster;

        public HoldLockActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        [Fact]
        public async Task Lock()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<IHoldLockActor>(Guid.NewGuid().ToString());
            var (isOk, lockId) = await lockActor.Lock();
            var successLockId = lockId;
            Assert.True(isOk);
            Assert.True(lockId > 0);

            (isOk, lockId) = await lockActor.Lock();
            Assert.False(isOk);
            Assert.True(lockId == 0);

            await lockActor.Unlock(successLockId);
            (isOk, lockId) = await lockActor.Lock();
            Assert.True(isOk);
            Assert.True(lockId > 0);
        }
        [Fact]
        public async Task Lock_Timeout()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<IHoldLockActor>(Guid.NewGuid().ToString());
            var (isOk, lockId) = await lockActor.Lock(5);
            Assert.True(isOk);
            Assert.True(lockId > 0);
            await Task.Delay(6 * 1000);
            (isOk, lockId) = await lockActor.Lock();
            Assert.True(isOk);
            Assert.True(lockId > 0);
        }
    }
}
