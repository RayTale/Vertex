using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.InnerService;

namespace Vertex.Runtime.InnerService
{
    [Reentrant]
    public class LockActor : Grain, ILockActor
    {
        private int locked;
        private readonly ConcurrentQueue<TaskCompletionSource<bool>> queue = new ConcurrentQueue<TaskCompletionSource<bool>>();

        public async Task<bool> Lock(int millisecondsDelay = 0)
        {
            if (Interlocked.CompareExchange(ref locked, 1, 0) == 0)
            {
                return true;
            }
            else
            {
                var taskSource = new TaskCompletionSource<bool>();
                if (millisecondsDelay != 0)
                {
                    using var tc = new CancellationTokenSource(millisecondsDelay);
                    tc.Token.Register(() =>
                    {
                        taskSource.TrySetResult(false);
                    });
                    queue.Enqueue(taskSource);
                    return await taskSource.Task;
                }
                queue.Enqueue(taskSource);
                return await taskSource.Task;
            }
        }

        public Task Unlock()
        {
            if (queue.TryDequeue(out var item))
            {
                if (!item.Task.IsCompleted)
                    item.TrySetResult(true);
                else
                    return Unlock();
            }
            else
            {
                Interlocked.CompareExchange(ref locked, 0, 1);
            }
            return Task.CompletedTask;
        }
    }
}
