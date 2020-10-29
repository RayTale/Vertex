using System.Threading.Tasks;
using Confluent.Kafka;
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
            await producer.Handler.ProduceAsync(this.topic, new Message<string, byte[]> { Value = bytes });
        }
    }
}
