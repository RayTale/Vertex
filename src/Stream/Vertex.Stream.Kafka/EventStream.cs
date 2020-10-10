using Confluent.Kafka;
using System.Threading.Tasks;
using Vertex.Abstractions.EventStream;

namespace Vertex.Stream.Kafka
{
    public class EventStream : IEventStream
    {
        private readonly IKafkaClient client;
        private readonly string topic;
        public EventStream(IKafkaClient client, string topic)
        {
            this.client = client;
            this.topic = topic;
        }
        public async ValueTask Next(byte[] bytes)
        {
            using var producer = this.client.GetProducer();
            await producer.Handler.ProduceAsync(topic, new Message<string, byte[]> { Value = bytes });
        }
    }
}
