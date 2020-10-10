using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vertex.Utils.Test.Hash
{
    public class ConsistentHash_Test
    {

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public void GetNode(int count)
        {
            var consHash = new ConsistentHash(Enumerable.Range(0, count).Select(o => o.ToString()));
            var nodes = new List<string>();
            for (int i = 0; i < count; i++)
            {
                nodes.Add(consHash.GetNode(i.ToString()));
            }
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(nodes[i], consHash.GetNode(i.ToString()));
            }
        }
    }
}
