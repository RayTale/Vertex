using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.InnerService;

namespace Vertex.Runtime.InnerService
{
    [Reentrant]
    public class LockActor : Grain, ILockActor
    {
        private readonly ConcurrentQueue<(TaskCompletionSource<bool> taskCompletionSource, int maxMillisecondsHold)> queue = new ConcurrentQueue<(TaskCompletionSource<bool> taskCompletionSource, int maxMillisecondsHold)>();
        private int locked;
        private long holdTimestamp;

        public async Task<bool> Lock(int millisecondsDelay = 0, int maxMillisecondsHold = 30 * 1000)
        {
            var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (this.locked == 1 && nowTimestamp > this.holdTimestamp)
            {
                Interlocked.CompareExchange(ref this.locked, 0, 1);
            }

            if (Interlocked.CompareExchange(ref this.locked, 1, 0) == 0)
            {
                this.holdTimestamp = nowTimestamp + maxMillisecondsHold;
                return true;
            }
            else
            {
                var taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (millisecondsDelay != 0)
                {
                    using var tc = new CancellationTokenSource(millisecondsDelay);
                    tc.Token.Register(() =>
                    {
                        taskSource.TrySetResult(false);
                    });
                    this.queue.Enqueue((taskSource, maxMillisecondsHold));
                    return await taskSource.Task;
                }
                this.queue.Enqueue((taskSource, maxMillisecondsHold));
                return await taskSource.Task;
            }
        }

        public Task Unlock()
        {
            if (this.queue.TryDequeue(out var item))
            {
                if (!item.taskCompletionSource.Task.IsCompleted)
                {
                    if (item.taskCompletionSource.TrySetResult(true))
                    {
                        this.holdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + item.maxMillisecondsHold;
                    }
                }
                else
                {
                    return this.Unlock();
                }
            }
            else
            {
                Interlocked.CompareExchange(ref this.locked, 0, 1);
            }
            return Task.CompletedTask;
        }
    }
}
