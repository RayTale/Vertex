using Orleans.TestingHost;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.InnerService;
using Vertex.Runtime.Core;
using Xunit;

namespace Vertex.Runtime.Test.InnerService
{
    [Collection(ClusterCollection.Name)]
    public class DIDActor_Test
    {
        private readonly TestCluster _cluster;

        public DIDActor_Test(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public async Task DIDActor(int count)
        {
            var idDict = new ConcurrentDictionary<long, bool>();
            var idActor = _cluster.GrainFactory.GetGrain<IDIDActor>("0");
            await Task.WhenAll(Enumerable.Range(0, count).Select(async i =>
            {
                var id = await idActor.NewID();
                Assert.True(idDict.TryAdd(id, true));
            }));
        }
    }
}
