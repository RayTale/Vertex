using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Xunit;

namespace Vertex.TxRuntime.Test.ActorTest
{
    [Collection(ClusterCollection.Name)]
    public class InnerTxActor_Test
    {
        private readonly TestCluster _cluster;
        public InnerTxActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async Task Tx_Begin_Timeout(int id)
        {
            var accountActor = _cluster.GrainFactory.GetGrain<IInnerTxAccount>(id);
            await accountActor.SetOptions(new Transaction.Options.VertexTxOptions { TxSecondsTimeout = 2 });
            await accountActor.BeginTx_Test();
            var ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await accountActor.BeginTx_Test();
            });
            Assert.True(ex is TimeoutException);
        }
        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public async Task Tx_Begin_Timeout_rollback(int id)
        {
            var accountActor = _cluster.GrainFactory.GetGrain<IInnerTxAccount>(id);
            await accountActor.SetOptions(new Transaction.Options.VertexTxOptions { TxSecondsTimeout = 2 });
            await accountActor.BeginTx_Test();
            await Task.Delay(2000);
            await accountActor.BeginTx_Test();
            Assert.True(true);
        }
        [Theory]
        [InlineData(200, 1)]
        [InlineData(201, 100)]
        [InlineData(202, 1000)]
        [InlineData(203, 1800)]
        public async Task Tx_Topup_Commit(int id, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IInnerTxAccount>(id);

            await accountActor.BeginTx_Test();
            await Task.WhenAll(Enumerable.Range(0, times).Select(i => accountActor.TopUp(topupAmount, guids[i])));
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * times);
            Assert.Equal(snapshot.Meta.Version, times);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            var backupSnapshot = await accountActor.GetBackupSnapshot();
            Assert.True(backupSnapshot.Data.Balance == 0);
            Assert.True(backupSnapshot.Meta.Version == 0);
            Assert.True(backupSnapshot.Meta.Version == 0);

            var eventDocuments = await accountActor.GetEventDocuments(1, times);
            Assert.True(eventDocuments.Count == 0);

            await accountActor.Commit_Test();

            eventDocuments = await accountActor.GetEventDocuments(1, times);
            Assert.True(eventDocuments.Count == times);
            Assert.True(Enumerable.Range(1, times).Sum() == eventDocuments.Sum(d => d.Version));
            backupSnapshot = await accountActor.GetBackupSnapshot();
            Assert.True(backupSnapshot.Data.Balance == 0);
            Assert.True(backupSnapshot.Meta.Version == 0);
            Assert.True(backupSnapshot.Meta.Version == 0);

            await accountActor.Finish_Test();

            backupSnapshot = await accountActor.GetBackupSnapshot();
            Assert.Equal(backupSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(backupSnapshot.Meta.Version, times);
            Assert.Equal(backupSnapshot.Meta.Version, backupSnapshot.Meta.DoingVersion);
        }
        [Theory]
        [InlineData(300, 1)]
        [InlineData(301, 100)]
        [InlineData(302, 1000)]
        [InlineData(303, 1800)]
        public async Task Tx_Topup_Rollback_1(int id, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IInnerTxAccount>(id);

            await accountActor.BeginTx_Test();
            await Task.WhenAll(Enumerable.Range(0, times).Select(i => accountActor.TopUp(topupAmount, guids[i])));
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * times);
            Assert.Equal(snapshot.Meta.Version, times);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            await accountActor.Rollbakc_Test();

            snapshot = await accountActor.GetSnapshot();
            Assert.True(snapshot.Data.Balance == 0);
            Assert.True(snapshot.Meta.Version == 0);
            Assert.True(snapshot.Meta.Version == 0);
        }
        [Theory]
        [InlineData(400, 1)]
        [InlineData(401, 100)]
        [InlineData(402, 1000)]
        [InlineData(403, 1800)]
        public async Task Tx_Topup_Rollback_2(int id, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = _cluster.GrainFactory.GetGrain<IInnerTxAccount>(id);

            await accountActor.BeginTx_Test();
            await Task.WhenAll(Enumerable.Range(0, times).Select(i => accountActor.TopUp(topupAmount, guids[i])));
            var snapshot = await accountActor.GetSnapshot();
            Assert.Equal(snapshot.Data.Balance, topupAmount * times);
            Assert.Equal(snapshot.Meta.Version, times);
            Assert.Equal(snapshot.Meta.Version, snapshot.Meta.DoingVersion);

            await accountActor.Commit_Test();

            var eventDocuments = await accountActor.GetEventDocuments(1, times);
            Assert.True(eventDocuments.Count == times);
            Assert.True(Enumerable.Range(1, times).Sum() == eventDocuments.Sum(d => d.Version));

            await accountActor.Rollbakc_Test();

            snapshot = await accountActor.GetSnapshot();
            Assert.True(snapshot.Data.Balance == 0);
            Assert.True(snapshot.Meta.Version == 0);
            Assert.True(snapshot.Meta.Version == 0);

            eventDocuments = await accountActor.GetEventDocuments(1, times);
            Assert.True(eventDocuments.Count == 0);
        }
    }
}
