using System;
using System.Threading.Tasks;
using Xunit;

namespace Vertex.Utils.Test
{
    public class ManualResetValueTaskSource_Test
    {
        [Fact]
        public async Task Create()
        {
            var source = new ManualResetValueTaskSource<int>();
            var valueTask = new ValueTask<int>(source, 0);
            source.SetResult(10);
            var result = await valueTask;
            Assert.True(result == 10);
        }

        [Fact]
        public async Task Reset_Failed()
        {
            var source = new ManualResetValueTaskSource<int>();
            var valueTask = new ValueTask<int>(source, 0);
            source.SetResult(10);
            var result = await valueTask;
            Assert.True(result == 10);
            source.Reset();
            var valueTask_1 = new ValueTask<int>(source, 0);
            source.SetResult(100);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                result = await valueTask_1;
            });
            Assert.True(ex is InvalidOperationException);
        }

        [Fact]
        public async Task Reset_Success()
        {
            var source = new ManualResetValueTaskSource<int>();
            var valueTask = new ValueTask<int>(source, source.Version);
            source.SetResult(10);
            var result = await valueTask;
            Assert.True(result == 10);
            source.Reset();
            var valueTask_1 = new ValueTask<int>(source, source.Version);
            source.SetResult(100);
            result = await valueTask_1;
            Assert.True(result == 100);
        }
    }
}
