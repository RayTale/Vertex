using BenchmarkDotNet.Attributes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Vertex.Utils.Benchmark.Channels
{
    [MemoryDiagnoser]
    public class MpscChannel
    {
        [Benchmark]
        public async Task BufferBlock_Test()
        {
           var  buffer = new BufferBlock<string>();
            var task = new TaskCompletionSource();
            ThreadPool.UnsafeQueueUserWorkItem(async state =>
            {
                while (await buffer.OutputAvailableAsync())
                {
                    while (buffer.TryReceive(out var _))
                    {
                    }
                }
                task.TrySetResult();
            }, null);
            for (int i = 0; i < 100000; i++)
            {
                var data = i.ToString();
                if (!buffer.Post(data))
                {
                    await buffer.SendAsync(data);
                }
            }
            buffer.Complete();
            await task.Task;
        }
        [Benchmark]
        public async Task Channel_Test()
        {
            var channel = Channel.CreateUnbounded<string>();
            var task = new TaskCompletionSource();
            ThreadPool.UnsafeQueueUserWorkItem(async state =>
            {
                while (await channel.Reader.WaitToReadAsync())
                {
                    while (channel.Reader.TryRead(out var _))
                    {
                    }
                }
                task.TrySetResult();
            }, null);
            for (int i = 0; i < 100000; i++)
            {
                var data = i.ToString();
                if (!channel.Writer.TryWrite(data))
                {
                    await channel.Writer.WriteAsync(data);
                }
            }
            channel.Writer.Complete();
            await task.Task;
        }
    }
}
