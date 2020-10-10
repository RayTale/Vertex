using Orleans.TestingHost;
using System.Threading.Tasks;
using Vertex.Abstractions.InnerService;
using Vertex.Runtime.Core;
using Xunit;

namespace Vertex.Runtime.Test.InnerService
{
    [Collection(ClusterCollection.Name)]
    public class WeightHoldLockActor_Test
    {
        private readonly TestCluster _cluster;

        public WeightHoldLockActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        [Fact]
        public async Task Lock()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<IWeightHoldLockActor>("0");
            var (isOk, lockId, delay) = await lockActor.Lock(10);
            var successLockId = lockId;
            Assert.True(isOk);
            Assert.True(delay == 0);
            Assert.True(lockId > 0);

            (isOk, lockId, delay) = await lockActor.Lock(9);
            Assert.False(isOk);
            Assert.True(delay == 0);
            Assert.True(lockId == 0);

            (isOk, lockId, delay) = await lockActor.Lock(11);
            Assert.False(isOk);
            Assert.True(delay > 0);
            Assert.True(lockId == 0);

            var holdResult = await lockActor.Hold(successLockId);
            Assert.False(holdResult);

            (isOk, successLockId, delay) = await lockActor.Lock(11);
            Assert.True(isOk);
            Assert.True(delay == 0);
            Assert.True(successLockId > 0);

            await lockActor.Unlock(successLockId);
            (isOk, lockId, delay) = await lockActor.Lock(10);
            Assert.True(isOk);
            Assert.True(delay == 0);
            Assert.True(lockId > 0);
        }
    }
}
