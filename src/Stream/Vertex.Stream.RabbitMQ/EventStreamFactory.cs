using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.EventStream;
using Vertex.Abstractions.Exceptions;
using Vertex.Stream.Common;
using Vertex.Stream.RabbitMQ.Client;
using Vertex.Utils;

namespace Vertex.Stream.RabbitMQ
{
    public class EventStreamFactory : IEventStreamFactory
    {
        private readonly ConcurrentDictionary<Type, StreamAttribute> typeAttributes = new ConcurrentDictionary<Type, StreamAttribute>();
        private readonly ConcurrentDictionary<Type, ConsistentHash> hashDict = new ConcurrentDictionary<Type, ConsistentHash>();
        private readonly ConcurrentDictionary<string, EventStream> streamDict = new ConcurrentDictionary<string, EventStream>();
        private readonly IGrainFactory grainFactory;
        private readonly IRabbitMQClient rabbitMQClient;

        public EventStreamFactory(IRabbitMQClient rabbitMQClient, IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
            this.rabbitMQClient = rabbitMQClient;
        }

        public ValueTask<IEventStream> Create<TPrimaryKey>(IActor<TPrimaryKey> actor)
        {
            var actorType = actor.GetType();
            var attribute = this.typeAttributes.GetOrAdd(actorType, key =>
            {
                var attributes = key.GetCustomAttributes(typeof(StreamAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes.First() as StreamAttribute;
                }
                else
                {
                    var noStreamAttributes = key.GetCustomAttributes(typeof(NoStreamAttribute), true);
                    if (noStreamAttributes.Length > 0)
                    {
                        return default;
                    }

                    throw new MissingAttributeException($"{nameof(StreamAttribute)} or {nameof(NoStreamAttribute)}=>{key.Name}");
                }
            });
            if (attribute != default)
            {
                var stream = attribute.ShardingFunc(actor.ActorId.ToString());
                var result = this.streamDict.GetOrAdd(stream, key =>
                {
                    return new EventStream(this.rabbitMQClient, attribute.Name, stream);
                });
                return ValueTask.FromResult(result as IEventStream);
            }

            return default;
        }
    }
}
