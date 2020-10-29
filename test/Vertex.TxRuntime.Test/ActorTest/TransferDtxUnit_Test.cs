using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans.TestingHost;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Vertex.TxRuntime.Test.Biz.Models;
using Xunit;

namespace Vertex.TxRuntime.Test.ActorTest
{
    [Collection(ClusterCollection.Name)]
    public class TransferDtxUnit_Test
    {
        private readonly TestCluster cluster;

        public TransferDtxUnit_Test(ClusterFixture fixture)
        {
            this.cluster = fixture.Cluster;
        }

        [Theory]
        [InlineData(3000, 4000, 1)]
        [InlineData(3001, 4001, 100)]
        [InlineData(3002, 4002, 1000)]
        [InlineData(3003, 4003, 1800)]
        public async Task Transfer_Success(int fromId, int toId, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var fromAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount>(fromId);
            var toAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount>(toId);
            var txUnit = this.cluster.GrainFactory.GetGrain<ITransferDtxUnit>(times);

            await Task.WhenAll(Enumerable.Range(0, times).Select(i => fromAccountActor.NoTxTopUp(topupAmount, guids[i])));
            var fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.Equal(fromSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(fromSnapshot.Meta.Version, times);
            Assert.Equal(fromSnapshot.Meta.Version, fromSnapshot.Meta.DoingVersion);

            var toSnapshot = await toAccountActor.GetSnapshot();
            Assert.True(toSnapshot.Data.Balance == 0);
            Assert.True(toSnapshot.Meta.Version == 0);
            Assert.True(toSnapshot.Meta.DoingVersion == 0);

            var results = await Task.WhenAll(Enumerable.Range(0, times).Select(i => txUnit.Ask(new TransferRequest
            {
                FromId = fromId,
                ToId = toId,
                Amount = topupAmount,
                Success = true,
                Id = $"trans0{guids[i]}"
            })));
            Assert.True(results.All(r => r));
            fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.True(fromSnapshot.Data.Balance == 0);
            Assert.True(fromSnapshot.Meta.Version == times * 2);
            Assert.True(fromSnapshot.Meta.DoingVersion == fromSnapshot.Meta.DoingVersion);

            toSnapshot = await toAccountActor.GetSnapshot();
            Assert.Equal(toSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(toSnapshot.Meta.Version, times);
            Assert.Equal(toSnapshot.Meta.Version, toSnapshot.Meta.DoingVersion);

            var unitDocuments = await txUnit.GetEventDocuments(1, times * 5);
            Assert.True(unitDocuments.Count == times * 3);
        }

        [Theory]
        [InlineData(30000, 40000, 2)]
        [InlineData(30001, 40001, 200)]
        [InlineData(30002, 40002, 2000)]
        [InlineData(30003, 40003, 2800)]
        public async Task Transfer_Rollback(int fromId, int toId, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var fromAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount>(fromId);
            var toAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount>(toId);
            var txUnit = this.cluster.GrainFactory.GetGrain<ITransferDtxUnit>(times);

            await Task.WhenAll(Enumerable.Range(0, times).Select(i => fromAccountActor.NoTxTopUp(topupAmount, guids[i])));
            var fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.Equal(fromSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(fromSnapshot.Meta.Version, times);
            Assert.Equal(fromSnapshot.Meta.Version, fromSnapshot.Meta.DoingVersion);

            var toSnapshot = await toAccountActor.GetSnapshot();
            Assert.True(toSnapshot.Data.Balance == 0);
            Assert.True(toSnapshot.Meta.Version == 0);
            Assert.True(toSnapshot.Meta.DoingVersion == 0);

            var results = await Task.WhenAll(Enumerable.Range(0, times).Select(i => txUnit.Ask(new TransferRequest
            {
                FromId = fromId,
                ToId = toId,
                Amount = topupAmount,
                Success = false,
                Id = $"trans1{guids[i]}"
            })));
            Assert.True(results.All(r => !r));
            fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.Equal(fromSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(fromSnapshot.Meta.Version, times);
            Assert.Equal(fromSnapshot.Meta.Version, fromSnapshot.Meta.DoingVersion);

            toSnapshot = await toAccountActor.GetSnapshot();
            Assert.True(toSnapshot.Data.Balance == 0);
            Assert.True(toSnapshot.Meta.Version == 0);
            Assert.True(toSnapshot.Meta.DoingVersion == 0);

            var unitDocuments = await txUnit.GetEventDocuments(1, times * 3);
            Assert.True(unitDocuments.Count == 0);
        }

        [Theory]
        [InlineData(300000, 400000, 3)]
        [InlineData(300001, 400001, 300)]
        [InlineData(300002, 400002, 3000)]
        [InlineData(300003, 400003, 3800)]
        public async Task Transfer_Error(int fromId, int toId, int times)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, times).Select(i => Guid.NewGuid().ToString()).ToList();
            var fromAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount>(fromId);
            var toAccountActor = this.cluster.GrainFactory.GetGrain<IDTxAccount_Error>(toId);
            var txUnit = this.cluster.GrainFactory.GetGrain<ITransferDtxUnit_Error>(times);

            await Task.WhenAll(Enumerable.Range(0, times).Select(i => fromAccountActor.NoTxTopUp(topupAmount, guids[i])));
            var fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.Equal(fromSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(fromSnapshot.Meta.Version, times);
            Assert.Equal(fromSnapshot.Meta.Version, fromSnapshot.Meta.DoingVersion);

            var toSnapshot = await toAccountActor.GetSnapshot();
            Assert.True(toSnapshot.Data.Balance == 0);
            Assert.True(toSnapshot.Meta.Version == 0);
            Assert.True(toSnapshot.Meta.DoingVersion == 0);

            var ex_1 = await Assert.ThrowsAsync<Exception>(async () =>
            {
                var results = await Task.WhenAll(Enumerable.Range(0, times).Select(i => txUnit.Ask(new TransferRequest
                {
                    FromId = fromId,
                    ToId = toId,
                    Amount = topupAmount,
                    Success = true,
                    Id = $"trans2{guids[i]}"
                })));
                Assert.True(results.All(r => !r));
            });
            Assert.True(ex_1 is Exception);

            fromSnapshot = await fromAccountActor.GetSnapshot();
            Assert.Equal(fromSnapshot.Data.Balance, topupAmount * times);
            Assert.Equal(fromSnapshot.Meta.Version, times);
            Assert.Equal(fromSnapshot.Meta.Version, fromSnapshot.Meta.DoingVersion);

            toSnapshot = await toAccountActor.GetSnapshot();
            Assert.True(toSnapshot.Data.Balance == 0);
            Assert.True(toSnapshot.Meta.Version == 0);
            Assert.True(toSnapshot.Meta.DoingVersion == 0);

            var unitDocuments = await txUnit.GetEventDocuments(1, times * 3);
            Assert.True(unitDocuments.Count == times * 2);
        }
    }
}
