namespace Vertex.Stream.RabbitMQ.Client
{
    public interface IRabbitMQClient
    {
        ModelWrapper PullModel();
    }
}
