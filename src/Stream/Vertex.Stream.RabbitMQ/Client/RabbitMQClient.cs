using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Vertex.Stream.RabbitMQ.Options;

namespace Vertex.Stream.RabbitMQ.Client
{
    public class RabbitMQClient : IRabbitMQClient
    {
        private readonly ConnectionFactory connectionFactory;
        private readonly RabbitOptions options;
        private readonly DefaultObjectPool<ModelWrapper> pool;

        public RabbitMQClient(IOptions<RabbitOptions> config)
        {
            this.options = config.Value;
            this.connectionFactory = new ConnectionFactory
            {
                UserName = this.options.UserName,
                Password = this.options.Password,
                VirtualHost = this.options.VirtualHost,
                AutomaticRecoveryEnabled = false
            };
            this.pool = new DefaultObjectPool<ModelWrapper>(new PooledModelPolicy(this.connectionFactory, this.options));
        }

        public ModelWrapper PullModel()
        {
            var result = this.pool.Get();
            if (result.Pool is null)
            {
                result.Pool = this.pool;
            }

            return result;
        }
    }
}
