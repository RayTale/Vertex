using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.InnerService;
using Vertex.Stream.Common;
using Vertex.Utils;

namespace Vertex.Stream.RabbitMQ.Consumer
{
    public class ConsumerManager : IHostedService, IDisposable
    {
        private const int HoldTime = 20 * 1000;
        private const int MonitTime = 60 * 2 * 1000;
        private const int CheckTime = 10 * 1000;
        private const int LockHoldingSeconds = 60;

        private readonly ILogger<ConsumerManager> logger;
        private readonly IServiceProvider provider;
        private readonly IGrainFactory grainFactory;
        private readonly List<QueueInfo> queues;
        private readonly ConcurrentDictionary<string, ConsumerRunner> consumerRunners = new ConcurrentDictionary<string, ConsumerRunner>();
        private readonly ConcurrentDictionary<string, long> runners = new ConcurrentDictionary<string, long>();
        private Timer heathCheckTimer;
        private Timer distributedMonitorTime;
        private Timer distributedHoldTimer;
        private int distributedMonitorTimeLock;
        private int distributedHoldTimerLock;
        private int heathCheckTimerLock;

        public ConsumerManager(
            ILogger<ConsumerManager> logger,
            IGrainFactory grainFactory,
            IServiceProvider provider)
        {
            this.provider = provider;
            this.logger = logger;
            this.grainFactory = grainFactory;
            this.queues = new List<QueueInfo>();
            foreach (var assembly in AssemblyHelper.GetAssemblies(logger))
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(StreamSubAttribute), false);
                    if (attributes.Length > 0 && attributes.First() is StreamSubAttribute attribute)
                    {
                        foreach (var i in Enumerable.Range(0, attribute.Sharding + 1))
                        {
                            var stream = i == 0 ? attribute.Name : $"{attribute.Name}_{i}";
                            var interfaceType = type.GetInterfaces().Where(t => typeof(IFlowActor).IsAssignableFrom(t) && !t.IsGenericType && t != typeof(IFlowActor)).FirstOrDefault();
                            var queue = i == 0 ? $"{attribute.Name}_{attribute.Group}" : $"{attribute.Name}_{attribute.Group}_{i}";
                            var existQueue = this.queues.SingleOrDefault(q => q.Exchange == attribute.Name && q.Queue == queue && q.RoutingKey == stream);
                            if (existQueue == default)
                            {
                                this.queues.Add(new QueueInfo
                                {
                                    SubActorType = new List<Type> { interfaceType },
                                    Exchange = attribute.Name,
                                    RoutingKey = stream,
                                    Queue = queue,
                                });
                            }
                            else
                            {
                                existQueue.SubActorType.Add(interfaceType);
                            }
                        }
                    }
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogInformation("EventBus Background Service is starting.");
            }

            this.distributedMonitorTime = new Timer(state => this.DistributedStart().Wait(), null, 1000, MonitTime);
            this.distributedHoldTimer = new Timer(state => this.DistributedHold().Wait(), null, HoldTime, HoldTime);
            this.heathCheckTimer = new Timer(state => { this.HeathCheck().Wait(); }, null, CheckTime, CheckTime);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation("EventBus Background Service is stopping.");
            }

            this.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation("EventBus Background Service is disposing.");
            }

            foreach (var runner in this.consumerRunners.Values)
            {
                runner.Close();
            }

            this.distributedMonitorTime?.Dispose();
            this.distributedHoldTimer?.Dispose();
            this.heathCheckTimer?.Dispose();
        }

        private async Task DistributedStart()
        {
            try
            {
                if (Interlocked.CompareExchange(ref this.distributedMonitorTimeLock, 1, 0) == 0)
                {
                    foreach (var queue in this.queues)
                    {
                        var key = queue.ToString();
                        if (!this.runners.ContainsKey(key))
                        {
                            var weight = 100000 - this.runners.Count;
                            while (true)
                            {
                                var (isOk, lockId, expectMillisecondDelay) = await this.grainFactory.GetGrain<IWeightHoldLockActor>(key).Lock(weight, LockHoldingSeconds);
                                if (isOk)
                                {
                                    if (this.runners.TryAdd(key, lockId))
                                    {
                                        var runner = new ConsumerRunner(this.provider, queue);
                                        this.consumerRunners.TryAdd(key, runner);
                                        await runner.Run();
                                        break;
                                    }
                                }
                                else if (expectMillisecondDelay > 0)
                                {
                                    await Task.Delay(expectMillisecondDelay);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    Interlocked.Exchange(ref this.distributedMonitorTimeLock, 0);
                    if (this.logger.IsEnabled(LogLevel.Information))
                    {
                        this.logger.LogInformation("EventBus Background Service is working.");
                    }
                }
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.InnerException ?? exception, nameof(this.DistributedStart));
                Interlocked.Exchange(ref this.distributedMonitorTimeLock, 0);
            }
        }

        private async Task DistributedHold()
        {
            try
            {
                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation("EventBus Background Service is holding.");
                }

                if (Interlocked.CompareExchange(ref this.distributedHoldTimerLock, 1, 0) == 0)
                {
                    foreach (var lockKV in this.runners)
                    {
                        if (this.runners.TryGetValue(lockKV.Key, out var lockId))
                        {
                            var holdResult = await this.grainFactory.GetGrain<IWeightHoldLockActor>(lockKV.Key).Hold(lockId, LockHoldingSeconds);
                            if (!holdResult)
                            {
                                if (this.consumerRunners.TryRemove(lockKV.Key, out var runner))
                                {
                                    runner.Close();
                                }

                                this.runners.TryRemove(lockKV.Key, out var _);
                            }
                        }
                    }

                    Interlocked.Exchange(ref this.distributedHoldTimerLock, 0);
                }
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.InnerException ?? exception, nameof(this.DistributedHold));
                Interlocked.Exchange(ref this.distributedHoldTimerLock, 0);
            }
        }

        private async Task HeathCheck()
        {
            try
            {
                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    this.logger.LogDebug("EventBus Background Service is checking.");
                }

                if (Interlocked.CompareExchange(ref this.heathCheckTimerLock, 1, 0) == 0)
                {
                    await Task.WhenAll(this.consumerRunners.Values.Select(runner => runner.HeathCheck()));
                    Interlocked.Exchange(ref this.heathCheckTimerLock, 0);
                }
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.InnerException ?? exception, nameof(this.HeathCheck));
                Interlocked.Exchange(ref this.heathCheckTimerLock, 0);
            }
        }
    }
}
