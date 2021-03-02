using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Vertex.Stream.Common;
using Vertex.Stream.RabbitMQ.Client;
using Vertex.Stream.RabbitMQ.Options;

namespace Vertex.Stream.RabbitMQ.Consumer
{
    public class ConsumerRunner
    {
        private static readonly TimeSpan WhileTimeoutSpan = TimeSpan.FromMilliseconds(100);
        private readonly IStreamSubHandler streamSubHandler;
        private readonly ConsumerOptions consumerOptions;
        private bool isFirst = true;
        private bool closed;

        public ConsumerRunner(
            IServiceProvider provider,
            QueueInfo queue)
        {
            this.Client = provider.GetService<IRabbitMQClient>();
            this.Logger = provider.GetService<ILogger<ConsumerRunner>>();
            this.streamSubHandler = provider.GetService<IStreamSubHandler>();
            this.consumerOptions = provider.GetService<IOptions<ConsumerOptions>>().Value;
            this.Queue = queue;
        }

        public ILogger<ConsumerRunner> Logger { get; }

        public IRabbitMQClient Client { get; }

        public QueueInfo Queue { get; }

        public ModelWrapper Model { get; set; }

        public bool IsUnAvailable => this.Model is null || this.Model.Model.IsClosed;

        public Task Run()
        {
            this.Model = this.Client.PullModel();
            if (this.isFirst)
            {
                this.isFirst = false;
                this.Model.Model.ExchangeDeclare(this.Queue.Exchange, "direct", true);
                this.Model.Model.QueueDeclare(this.Queue.Queue, true, false, false, null);
                this.Model.Model.QueueBind(this.Queue.Queue, this.Queue.Exchange, this.Queue.RoutingKey);
            }

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
                                var whileResult =
                                    this.Model.Model.BasicGet(this.Queue.Queue, this.consumerOptions.AutoAck);
                                if (whileResult is null)
                                {
                                    break;
                                }
                                else
                                {
                                    list.Add(new BytesBox(whileResult.Body, whileResult));
                                }

                                if ((DateTimeOffset.UtcNow - batchStartTime).TotalMilliseconds >
                                    this.Model.Connection.Options.CunsumerMaxMillisecondsInterval ||
                                    list.Count == this.Model.Connection.Options.CunsumerMaxBatchSize)
                                {
                                    break;
                                }
                            }

                            if (list.Any())
                            {
                                await this.Notice(list);
                            }
                            else
                            {
                                await Task.Delay(WhileTimeoutSpan);
                            }
                        }
                        catch (Exception exception)
                        {
                            this.Logger.LogError(exception.InnerException ?? exception,
                                $"An error occurred in {this.Queue}");
                            foreach (var item in list.Where(o => !o.Success))
                            {
                                this.Model.Model.BasicReject(((BasicGetResult) item.Origin).DeliveryTag, true);
                            }
                        }
                        finally
                        {
                            list = list.Where(o => o.Success).ToList();
                            if (list.Any())
                            {
                                var maxDeliveryTag = list.Max(o => ((BasicGetResult)o.Origin).DeliveryTag);
                                if (maxDeliveryTag > 0)
                                {
                                    this.Model.Model.BasicAck(maxDeliveryTag, true);
                                }
                            }
                        }
                    }
                }, null);
            return Task.CompletedTask;
        }

        private async Task Notice(List<BytesBox> list, int times = 0)
        {
            try
            {
                if (list.Count > 1)
                {
                    await Task.WhenAll(this.Queue.SubActorType.Select(subType =>
                        this.streamSubHandler.EventHandler(subType, list)));
                }
                else if (list.Count == 1)
                {
                    await Task.WhenAll(this.Queue.SubActorType.Select(subType =>
                        this.streamSubHandler.EventHandler(subType, list[0])));
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

        public Task HeathCheck()
        {
            if (this.IsUnAvailable)
            {
                this.Close();
                return this.Run();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public void Close()
        {
            this.closed = true;
            this.Model?.Dispose();
        }
    }
}