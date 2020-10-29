using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans.TestingHost;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Xunit;

namespace Vertex.TxRuntime.Test.ActorTest
{
    [Collection(ClusterCollection.Name)]
    public class ReentryActor_Test
    {
        private readonly TestCluster cluster;

        public ReentryActor_Test(ClusterFixture fixture)
        {
            this.cluster = fixture.Cluster;
        }

        [Theory]
        [InlineData(200, 1)]
        [InlineData(201, 100)]
        [InlineData(202, 1000)]
        [InlineData(203, 1800)]
        public async Task Concurrent_Topup(int id, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = this.cluster.GrainFactory.GetGrain<IReentryAccount>(id);

            await Task.WhenAll(Enumerable.Range(0, times).Select(i => accountActor.TopUp(topupAmount, guids[i])));
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * times);
            Assert.Equal(snapshot.Meta.Version, times);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            var eventDocuments = await accountActor.GetEventDocuments(1, times);
            Assert.True(eventDocuments.Count == times);
            Assert.True(Enumerable.Range(1, times).Sum() == eventDocuments.Sum(d => d.Version));

            var backupSnapshot = await accountActor.GetBackupSnapshot();
            Assert.Equal(backupSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(backupSnapshot.Meta.Version, times);
            Assert.Equal(backupSnapshot.Meta.Version, backupSnapshot.Meta.DoingVersion);
        }

        [Theory]
        [InlineData(300)]
        [InlineData(301)]
        [InlineData(302)]
        [InlineData(303)]
        public async Task Concurrent_ErrorEvent(int id)
        {
            decimal topupAmount = 100;
            var accountActor = this.cluster.GrainFactory.GetGrain<IReentryAccount>(id);

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await accountActor.ErrorTest();
            });
            Assert.True(ex is ArgumentException);

            var ex_1 = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await accountActor.TopUp(topupAmount, Guid.NewGuid().ToString());
            });
            Assert.True(ex_1 is ArgumentException);
        }
    }
}
