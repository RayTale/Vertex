using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Vertex.Utils.PooledTask;

namespace Vertex.Utils.Benchmark.TaskSource
{
    [MemoryDiagnoser]
    public class TaskSourceBenchmark
    {
        private static readonly TaskSourcePool<int> TaskSourcePool = new TaskSourcePool<int>();

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public async Task TaskSource(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var source = new TaskCompletionSource<int>();
                source.TrySetResult(i);
                await source.Task;
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public async Task ManualTaskSource(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var source = new ManualResetValueTaskSource<int>();
                source.SetResult(i);
                await source.AsTask();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public async Task PolledManualTaskSource(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var source = TaskSourcePool.Get();
                source.SetResult(i);
                await source.AsTask();
                TaskSourcePool.Return(source);
            }
        }
    }
}
