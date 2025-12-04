using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.EventStream;
using Vertex.Abstractions.Exceptions;
using Vertex.Stream.Common;

namespace Vertex.Stream.Kafka
{
    public class EventStreamFactory : IEventStreamFactory
    {
        private readonly ConcurrentDictionary<Type, StreamAttribute> typeAttributes = new();
        private readonly ConcurrentDictionary<string, EventStream> streamDict = new();
        private readonly IKafkaClient client;

        public EventStreamFactory(IKafkaClient client)
        {
            this.client = client;
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
                    return new EventStream(this.client, key);
                });
                return ValueTask.FromResult(result as IEventStream);
            }
            return default;
        }
    }
}
