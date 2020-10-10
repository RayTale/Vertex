using Microsoft.Extensions.DependencyInjection;
using System;
using Vertex.Abstractions.EventStream;
using Vertex.Stream.Common;
using Vertex.Stream.RabbitMQ.Client;
using Vertex.Stream.RabbitMQ.Consumer;
using Vertex.Stream.RabbitMQ.Options;

namespace Vertex.Stream.RabbitMQ
{
    public static class Extensions
    {
        public static void AddRabbitMQStream(this IServiceCollection serviceCollection, Action<RabbitOptions> configAction)
        {
            serviceCollection.Configure<RabbitOptions>(config => configAction(config));
            serviceCollection.AddSingleton<IEventStreamFactory, EventStreamFactory>();
            serviceCollection.AddSingleton<IRabbitMQClient, RabbitMQClient>();
            serviceCollection.AddSingleton<IStreamSubHandler, StreamSubHandler>();
            serviceCollection.AddHostedService<ConsumerManager>();
        }
    }
}
