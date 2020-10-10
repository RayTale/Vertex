using Orleans.TestingHost;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.InnerService;
using Vertex.Runtime.Core;
using Xunit;

namespace Vertex.Runtime.Test.InnerService
{
    [Collection(ClusterCollection.Name)]
    public class LockActor_Test
    {
        private readonly TestCluster _cluster;

        public LockActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        [Fact]
        public async Task LockActor()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<ILockActor>("0");
            var lockResult = await lockActor.Lock(30 * 1000);
            Assert.True(lockResult);
            var lockTask = lockActor.Lock(30 * 1000);
            await Task.WhenAny(Task.Delay(5 * 1000), lockTask);
            Assert.False(lockTask.IsCompletedSuccessfully);
            await lockActor.Unlock();
            await Task.WhenAny(Task.Delay(5 * 1000), lockTask);
            Assert.True(lockTask.IsCompletedSuccessfully);
            await lockActor.Unlock();
        }
        [Fact]
        public async Task LockActor_Timeout()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<ILockActor>("2");
            var lockResult = await lockActor.Lock(3 * 1000);
            Assert.True(lockResult);
            var lockTask = lockActor.Lock(2 * 1000);
            await Task.WhenAny(Task.Delay(3 * 1000), lockTask);
            Assert.True(lockTask.IsCompletedSuccessfully);
            Assert.False(lockTask.Result);
            await lockActor.Unlock();
        }
        [Fact]
        public async Task LockActor_Timeout_0()
        {
            var lockActor = _cluster.GrainFactory.GetGrain<ILockActor>("3");
            var lockResult = await lockActor.Lock(3 * 1000);
            Assert.True(lockResult);
            var lockTask = lockActor.Lock();
            await Task.WhenAny(Task.Delay(3 * 1000), lockTask);
            Assert.False(lockTask.IsCompletedSuccessfully);
            await lockActor.Unlock();
        }
        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task LockActor_Concurrent(int count)
        {
            var lockActor = _cluster.GrainFactory.GetGrain<ILockActor>("1");
            var lockResult = await lockActor.Lock(30 * 1000);
            Assert.True(lockResult);
            var taskList = Enumerable.Range(0, count).Select(i => lockActor.Lock(30 * 1000)).ToList();
            await Task.WhenAny(Task.WhenAll(taskList), Task.Delay(1000));
            Assert.False(taskList.Where(t => t.IsCompletedSuccessfully).Any());
            await lockActor.Unlock();
            await Task.WhenAny(Task.WhenAll(taskList), Task.Delay(1000));
            Assert.True(taskList.Where(t => t.IsCompletedSuccessfully).Count() == 1);
            for (int i = 0; i < count; i++)
            {
                await lockActor.Unlock();
            }
        }
    }
}
