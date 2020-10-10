using System.Threading.Tasks;
using Vertex.Abstractions.EventStream;
using Vertex.Stream.RabbitMQ.Client;

namespace Vertex.Stream.RabbitMQ
{
    public class EventStream : IEventStream
    {
        private readonly IRabbitMQClient rabbitMQClient;
        private readonly string exchange, routingKey;
        public EventStream(IRabbitMQClient rabbitMQClient, string exchange, string routingKey)
        {
            this.rabbitMQClient = rabbitMQClient;
            this.exchange = exchange;
            this.routingKey = routingKey;
        }
        public ValueTask Next(byte[] bytes)
        {
            using var model = this.rabbitMQClient.PullModel();
            model.Publish(bytes, exchange, routingKey, false);
            return ValueTask.CompletedTask;
        }
    }
}
