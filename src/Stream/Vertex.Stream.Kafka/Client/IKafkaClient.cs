namespace Vertex.Stream.Kafka
{
    public interface IKafkaClient
    {
        PooledConsumer GetConsumer(string group);

        PooledProducer GetProducer();
    }
}
