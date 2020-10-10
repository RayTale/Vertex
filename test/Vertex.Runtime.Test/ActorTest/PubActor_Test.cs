using Orleans.Runtime;
using Orleans.TestingHost;
using System;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Runtime.Actor;
using Vertex.Runtime.Core;
using Vertex.Runtime.Test.IActors;
using Xunit;

namespace Vertex.Runtime.Test.ActorTest
{
    [Collection(ClusterCollection.Name)]
    public class PubActor_Test
    {
        private readonly TestCluster _cluster;
        public PubActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        /// <summary>
        /// 普通事件提交测试
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(100, 1)]
        [InlineData(500, 2)]
        [InlineData(1000, 3)]
        [InlineData(3000, 4)]
        public async Task RaiseEvent(int count, int id)
        {
            decimal topupAmount = 100;
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);
            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, Guid.NewGuid().ToString())));
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
        }
        /// <summary>
        /// 幂等性测试
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(100, 50)]
        [InlineData(500, 51)]
        [InlineData(1000, 52)]
        [InlineData(3000, 53)]
        public async Task RaiseEvent_Idempotency(int count, int id)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);
            //相同的FlowId多次请求只会生效一次，幂等性保证
            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));
            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
        }
        [Theory]
        [InlineData(100, 100)]
        [InlineData(500, 101)]
        [InlineData(1000, 102)]
        [InlineData(3000, 103)]
        public async Task RecoverySnapshot(int count, int id)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            await accountActor.RecoverySnapshot_Test();
            snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
        }
        [Theory]
        [InlineData(100, 200)]
        [InlineData(500, 201)]
        [InlineData(800, 202)]
        [InlineData(3200, 203)]
        public async Task Deactivate(int count, int id)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
            Assert.False(snapshot.Meta.IsLatest);
            var actorOptions = await accountActor.GetOptions();

            var activateSnapshotVersion = await accountActor.GetActivateSnapshotVersion();
            Assert.True(count - count % actorOptions.SnapshotVersionInterval == activateSnapshotVersion);
            var isLatest = (snapshot.Meta.Version - activateSnapshotVersion) > actorOptions.MinSnapshotVersionInterval;
            await accountActor.Deactivate_Test();
            snapshot = await accountActor.GetSnapshot();
            Assert.True(snapshot.Data.Balance == 0);
            Assert.True(snapshot.Meta.Version == 0);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            await accountActor.RecoverySnapshot_Test();
            snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
            Assert.True(snapshot.Meta.IsLatest == isLatest);
        }
        [Theory]
        [InlineData(100, 300)]
        [InlineData(500, 301)]
        [InlineData(800, 302)]
        [InlineData(3200, 303)]
        public async Task Archive(int count, int id)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);

            var (can, endTimestamp) = await accountActor.CanArchive_Test();
            Assert.False(can);
            Assert.True(endTimestamp == 0);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            (can, endTimestamp) = await accountActor.CanArchive_Test();
            Assert.False(can);
            Assert.True(endTimestamp == 0);

            await accountActor.SetArchiveOptions(new Options.ArchiveOptions { MinIntervalSeconds = 1, RetainSeconds = 1 });
            await Task.Delay(2000);
            (can, endTimestamp) = await accountActor.CanArchive_Test();
            Assert.True(can);
            Assert.True(endTimestamp > 0);

            await accountActor.Archive_Test(endTimestamp);
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * count);
            Assert.Equal(snapshot.Meta.Version, count);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);
            Assert.True(snapshot.Meta.IsLatest);
            Assert.True(snapshot.Meta.MinEventVersion == snapshot.Meta.Version + 1);
            Assert.True(snapshot.Meta.MinEventTimestamp >= endTimestamp);

            var originEvents = await accountActor.GetEventDocuments_FromEventStorage(1, count);
            Assert.True(originEvents.Count == 0);

            var archiveEventsCount = await accountActor.GetArchiveEventCount();
            Assert.True(archiveEventsCount == count);

            var documents = await accountActor.GetEventDocuments(0, count);
            Assert.True(documents.Count == count);
            Assert.True(Enumerable.Range(1, count).Sum() == documents.Sum(d => d.Version));

            documents = await accountActor.GetEventDocuments(0, 100000);
            Assert.True(documents.Count == count);
            Assert.True(Enumerable.Range(1, count).Sum() == documents.Sum(d => d.Version));

            documents = await accountActor.GetEventDocuments(0, 10);
            Assert.True(documents.Count == 10);
            Assert.True(Enumerable.Range(1, 10).Sum() == documents.Sum(d => d.Version));
        }
        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(402)]
        [InlineData(403)]
        public async Task EventHandler_Error(int id)
        {
            decimal topupAmount = 100;
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await accountActor.HandlerError_Test();
            });
            Assert.True(ex is ArgumentException);
            var topupEx = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await accountActor.TopUp(topupAmount, Guid.NewGuid().ToString());
            });
            Assert.True(topupEx is ArgumentException);
        }
        [Theory]
        [InlineData(500)]
        [InlineData(501)]
        [InlineData(502)]
        [InlineData(503)]
        public async Task FollowId(int id)
        {
            decimal topupAmount = 100;
            var accountActor = _cluster.GrainFactory.GetGrain<IAccount>(id);
            var topupEx = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await accountActor.TopUp(topupAmount);
            });
            Assert.True(topupEx is ArgumentNullException);

            RequestContext.Set(ActorConsts.eventFlowIdKey, Guid.NewGuid().ToString());
            var result = await accountActor.TopUp(topupAmount);
            Assert.True(result);
        }
    }
}
