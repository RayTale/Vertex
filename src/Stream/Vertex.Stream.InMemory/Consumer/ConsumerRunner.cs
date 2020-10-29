using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Vertex.Stream.Common;
using Vertex.Stream.InMemory.IGrains;
using Vertex.Stream.InMemory.Options;

namespace Vertex.Stream.InMemory.Consumer
{
    public class ConsumerRunner
    {
        private readonly IStreamSubHandler streamSubHandler;
        private readonly IStreamProvider streamProvider;
        private readonly IGrainFactory grainFactory;
        private readonly ConsumerOptions consumerOptions;
        private StreamSubscriptionHandle<byte[]> streamSubscriptionHandle;
        private Guid streamId;
        private bool closed;

        public ConsumerRunner(
            IStreamProvider streamProvider,
            IServiceProvider provider,
            QueueInfo queue)
        {
            this.streamProvider = streamProvider;
            this.grainFactory = provider.GetService<IGrainFactory>();
            this.Logger = provider.GetService<ILogger<ConsumerRunner>>();
            this.streamSubHandler = provider.GetService<IStreamSubHandler>();
            this.consumerOptions = provider.GetService<IOptions<ConsumerOptions>>().Value;
            this.Queue = queue;
        }

        public ILogger<ConsumerRunner> Logger { get; }

        public QueueInfo Queue { get; }

        public async Task Run()
        {
            if (this.streamSubscriptionHandle != default)
            {
                await this.streamSubscriptionHandle.UnsubscribeAsync();
            }
            this.streamId = await this.grainFactory.GetGrain<IStreamIdActor>(0).GetId(this.Queue.Topic);
            var stream = this.streamProvider.GetStream<byte[]>(this.streamId, this.Queue.Name);
            this.streamSubscriptionHandle = await stream.SubscribeAsync(async bytesList => await this.Notice(bytesList.Select(o => new BytesBox(o.Item, default)).ToList()));
        }

        private async Task Notice(List<BytesBox> list, int times = 0)
        {
            try
            {
                if (list.Count > 1)
                {
                    await this.streamSubHandler.EventHandler(this.Queue.ActorType, list);
                }
                else if (list.Count == 1)
                {
                    await this.streamSubHandler.EventHandler(this.Queue.ActorType, list[0]);
                }
            }
            catch
            {
                if (this.consumerOptions.RetryCount >= times)
                {
                    await Task.Delay(this.consumerOptions.RetryIntervals);
                    await this.Notice(list.Where(o => !o.Success).ToList(), times + 1);
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task HeathCheck()
        {
            var checkStreamId = await this.grainFactory.GetGrain<IStreamIdActor>(0).GetId(this.Queue.Topic);
            if (!this.closed && checkStreamId != this.streamId)
            {
                await this.Run();
            }
        }

        public void Close()
        {
            this.closed = true;
        }
    }
}
