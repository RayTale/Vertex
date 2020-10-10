using Orleans;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.EventStream;
using Vertex.Abstractions.Exceptions;
using Vertex.Abstractions.Actor;
using Vertex.Stream.Common;
using Vertex.Stream.RabbitMQ.Client;
using Vertex.Utils;

namespace Vertex.Stream.RabbitMQ
{
    public class EventStreamFactory : IEventStreamFactory
    {
        readonly ConcurrentDictionary<Type, StreamAttribute> typeAttributes = new ConcurrentDictionary<Type, StreamAttribute>();
        readonly ConcurrentDictionary<Type, ConsistentHash> hashDict = new ConcurrentDictionary<Type, ConsistentHash>();
        readonly ConcurrentDictionary<string, EventStream> streamDict = new ConcurrentDictionary<string, EventStream>();
        readonly IGrainFactory grainFactory;
        readonly IRabbitMQClient rabbitMQClient;
        public EventStreamFactory(IRabbitMQClient rabbitMQClient, IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
            this.rabbitMQClient = rabbitMQClient;
        }
        public ValueTask<IEventStream> Create<PrimaryKey>(IActor<PrimaryKey> actor)
        {
            var actorType = actor.GetType();
            var attribute = typeAttributes.GetOrAdd(actorType, key =>
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
                        return default;
                    throw new MissingAttributeException($"{nameof(StreamAttribute)} or {nameof(NoStreamAttribute)}=>{key.Name}");
                }
            });
            if (attribute != default)
            {
                var stream = attribute.ShardingFunc(actor.ActorId.ToString());
                var result = streamDict.GetOrAdd(stream, key =>
                {
                    return new EventStream(rabbitMQClient, attribute.Name, stream);
                });
                return ValueTask.FromResult(result as IEventStream);
            }
            return default;
        }
    }
}
