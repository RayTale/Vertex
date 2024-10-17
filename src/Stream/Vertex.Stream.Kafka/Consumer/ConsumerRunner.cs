using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vertex.Stream.Common;
using Vertex.Stream.Kafka.Options;

namespace Vertex.Stream.Kafka.Consumer
{
    public class ConsumerRunner
    {
        private static readonly TimeSpan WhileTimeoutSpan = TimeSpan.FromSeconds(30);
        private readonly IStreamSubHandler streamSubHandler;
        private readonly ConsumerOptions consumerOptions;

        private bool closed;
        private bool available;

        public ConsumerRunner(
            IServiceProvider provider,
            QueueInfo queue)
        {
            this.Client = provider.GetService<IKafkaClient>();
            this.Logger = provider.GetService<ILogger<ConsumerRunner>>();
            this.streamSubHandler = provider.GetService<IStreamSubHandler>();
            this.consumerOptions = provider.GetService<IOptions<ConsumerOptions>>().Value;
            this.Queue = queue;
        }

        public ILogger<ConsumerRunner> Logger { get; }

        public IKafkaClient Client { get; }

        public QueueInfo Queue { get; }

        public Task Run()
        {
            var consumer = this.Client.GetConsumer(this.Queue.Group);
            consumer.Handler.Subscribe(this.Queue.Topic);
            this.available = true;
            ThreadPool.UnsafeQueueUserWorkItem(
                async state =>
            {
                while (!this.closed)
                {
                    var list = new List<BytesBox>();
                    var batchStartTime = DateTimeOffset.UtcNow;
                    try
                    {
                        while (true)
                        {
                            var whileResult = consumer.Handler.Consume(WhileTimeoutSpan);
                            if (whileResult is null || whileResult.IsPartitionEOF || whileResult.Message.Value == null)
                            {
                                break;
                            }
                            else
                            {
                                list.Add(new BytesBox(whileResult.Message.Value, whileResult));
                            }

                            if ((DateTimeOffset.UtcNow - batchStartTime).TotalMilliseconds > consumer.MaxMillisecondsInterval || list.Count == consumer.MaxBatchSize)
                            {
                                break;
                            }
                        }

                        await this.Notice(list);
                    }
                    catch (Exception exception)
                    {
                        if (exception is ConsumeException consumerEx && consumerEx.Error.Code == ErrorCode.UnknownTopicOrPart)
                        {
                            this.Logger.LogInformation("topic=>{0} is not initialized", this.Queue.Topic);
                        }
                        else
                        {
                            this.Logger.LogError(exception.InnerException ?? exception, $"An error occurred in {this.Queue.Topic}");
                        }
                        using var producer = this.Client.GetProducer();
                        foreach (var item in list.Where(o => !o.Success))
                        {
                            var result = (ConsumeResult<string, byte[]>)item.Origin;
                            await producer.Handler.ProduceAsync(this.Queue.Topic, new Message<string, byte[]> { Key = result.Message.Key, Value = result.Message.Value });
                        }
                    }
                    finally
                    {
                        if (list.Count > 0)
                        {
                            try
                            {
                                this.Logger.LogInformation($"数量={list.Count}");
                                consumer.Handler.Commit();
                            }
                            catch (Exception ex)
                            {
                                this.Logger.LogError(ex.Message);
                            }
                        }
                    }
                }
                this.available = false;
                consumer.Handler.Unsubscribe();
                consumer.Dispose();
            }, null);
            return Task.CompletedTask;
        }

        private async Task Notice(List<BytesBox> list, int times = 0)
        {
            try
            {
                if (list.Count > 1)
                {
                    await Task.WhenAll(this.Queue.SubActorType.Select(subType => this.streamSubHandler.EventHandler(subType, list)));
                }
                else if (list.Count == 1)
                {
                    await Task.WhenAll(this.Queue.SubActorType.Select(subType => this.streamSubHandler.EventHandler(subType, list[0])));
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
            if (!this.closed && !this.available)
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
