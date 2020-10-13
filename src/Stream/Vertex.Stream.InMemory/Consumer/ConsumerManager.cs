using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.InnerService;
using Vertex.Stream.Common;
using Vertex.Stream.InMemory.Options;
using Vertex.Utils;

namespace Vertex.Stream.InMemory.Consumer
{
    public class ConsumerManager : IHostedService, IDisposable
    {
        private readonly List<QueueInfo> queues;
        readonly StreamOptions streamOptions;
        private readonly ILogger<ConsumerManager> logger;
        private readonly IServiceProvider provider;
        private readonly IGrainFactory grainFactory;

        private readonly ConcurrentDictionary<string, ConsumerRunner> consumerRunners = new ConcurrentDictionary<string, ConsumerRunner>();

        private readonly ConcurrentDictionary<string, long> runners = new ConcurrentDictionary<string, long>();

        private Timer heathCheckTimer, distributedMonitorTime, distributedHoldTimer;

        private const int lockHoldingSeconds = 60;
        private int distributedMonitorTimeLock;
        private int distributedHoldTimerLock;
        private int heathCheckTimerLock;
        private const int holdTime = 20 * 1000;
        private const int monitTime = 60 * 2 * 1000;
        private const int checkTime = 10 * 1000;

        public ConsumerManager(
            ILogger<ConsumerManager> logger,
            IOptions<StreamOptions> streamOptions,
            IGrainFactory grainFactory,
            IServiceProvider provider)
        {
            this.provider = provider;
            this.streamOptions = streamOptions.Value;
            this.logger = logger;
            this.grainFactory = grainFactory;

            queues = new List<QueueInfo>();
            foreach (var assembly in AssemblyHelper.GetAssemblies(logger))
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(StreamSubAttribute), false);
                    if (attributes.Length > 0 && attributes.First() is StreamSubAttribute attribute)
                    {
                        foreach (var i in Enumerable.Range(0, attribute.Sharding + 1))
                        {
                            var topic = i == 0 ? attribute.Name : $"{attribute.Name}_{i}";
                            var interfaceType = type.GetInterfaces().Where(t => typeof(IFlowActor).IsAssignableFrom(t) && !t.IsGenericType && t != typeof(IFlowActor)).FirstOrDefault();
                            queues.Add(new QueueInfo
                            {
                                ActorType = interfaceType,
                                Topic = topic,
                                Name = attribute.Name,
                                Group = attribute.Group
                            });
                        }
                    }
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation("EventBus Background Service is starting.");
            }

            this.distributedMonitorTime = new Timer(state => this.DistributedStart().Wait(), null, 1000, monitTime);
            this.distributedHoldTimer = new Timer(state => this.DistributedHold().Wait(), null, holdTime, holdTime);
            this.heathCheckTimer = new Timer(state => { this.HeathCheck().Wait(); }, null, checkTime, checkTime);
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

            this.heathCheckTimer?.Dispose();
            this.distributedMonitorTime?.Dispose();
            this.distributedHoldTimer?.Dispose();
        }
        private async Task DistributedStart()
        {
            try
            {
                var streamProvider = provider.GetRequiredServiceByName<IStreamProvider>(streamOptions.ProviderName);
                if (Interlocked.CompareExchange(ref this.distributedMonitorTimeLock, 1, 0) == 0)
                {
                    foreach (var queue in this.queues)
                    {
                        var key = queue.ToString();
                        if (!this.runners.ContainsKey(key))
                        {
                            var weight = 100000 - this.runners.Count;
                            var (isOk, lockId, expectMillisecondDelay) = await this.grainFactory.GetGrain<IWeightHoldLockActor>(key).Lock(weight, lockHoldingSeconds);
                            if (isOk)
                            {
                                if (this.runners.TryAdd(key, lockId))
                                {
                                    var runner = new ConsumerRunner(streamProvider, this.provider, queue);
                                    this.consumerRunners.TryAdd(key, runner);
                                    await runner.Run();
                                }

                            }
                        }
                    }

                    Interlocked.Exchange(ref this.distributedMonitorTimeLock, 0);
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
                if (Interlocked.CompareExchange(ref this.distributedHoldTimerLock, 1, 0) == 0)
                {
                    foreach (var lockKV in this.runners)
                    {
                        if (this.runners.TryGetValue(lockKV.Key, out var lockId))
                        {
                            var holdResult = await this.grainFactory.GetGrain<IWeightHoldLockActor>(lockKV.Key).Hold(lockId, lockHoldingSeconds);
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
