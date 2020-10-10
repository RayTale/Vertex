using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using System;
using Vertex.Abstractions.EventStream;
using Vertex.Stream.Common;
using Vertex.Stream.Kafka.Consumer;
using Vertex.Stream.Kafka.Options;

namespace Vertex.Stream.Kafka
{
    public static class Extensions
    {
        public static void AddKafkaStream(
            this IServiceCollection serviceCollection,
            Action<KafkaOptions> configAction,
            Action<ProducerConfig> producerConfigAction,
            Action<ConsumerConfig> consumerConfigAction)
        {
            serviceCollection.Configure<ProducerConfig>(config => producerConfigAction(config));
            serviceCollection.Configure<ConsumerConfig>(config => consumerConfigAction(config));
            serviceCollection.Configure<KafkaOptions>(config => configAction(config));
            serviceCollection.AddSingleton<IEventStreamFactory, EventStreamFactory>();
            serviceCollection.AddSingleton<IKafkaClient, KafkaClient>();
            serviceCollection.AddSingleton<IStreamSubHandler, StreamSubHandler>();
            serviceCollection.AddHostedService<ConsumerManager>();
        }
    }
}
