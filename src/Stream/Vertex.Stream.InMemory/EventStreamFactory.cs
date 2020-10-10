using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.EventStream;
using Vertex.Abstractions.Exceptions;
using Vertex.Stream.Common;
using Vertex.Stream.InMemory.IGrains;
using Vertex.Stream.InMemory.Options;
using Vertex.Utils;

namespace Vertex.Stream.InMemory
{
    public class EventStreamFactory : IEventStreamFactory
    {
        readonly IServiceProvider serviceProvider;
        readonly IGrainFactory grainFactory;
        readonly StreamOptions streamOptions;
        readonly ConcurrentDictionary<Type, StreamAttribute> typeAttributes = new ConcurrentDictionary<Type, StreamAttribute>();
        readonly ConcurrentDictionary<Type, ConsistentHash> hashDict = new ConcurrentDictionary<Type, ConsistentHash>();
        readonly ConcurrentDictionary<string, EventStream> streamDict = new ConcurrentDictionary<string, EventStream>();
        public EventStreamFactory(
            IServiceProvider serviceProvider,
            IGrainFactory grainFactory,
            IOptions<StreamOptions> options)
        {
            this.serviceProvider = serviceProvider;
            this.grainFactory = grainFactory;
            this.streamOptions = options.Value;
        }
        public async ValueTask<IEventStream> Create<PrimaryKey>(IActor<PrimaryKey> actor)
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
                var streamId = await grainFactory.GetGrain<IStreamIdActor>(0).GetId(stream);
                var result = streamDict.GetOrAdd(stream, key =>
                {
                    var streamProvider = serviceProvider.GetRequiredServiceByName<IStreamProvider>(streamOptions.ProviderName);

                    return new EventStream(streamProvider.GetStream<byte[]>(streamId, attribute.Name));
                });
                return result;
            }
            else
                return default;
        }
    }
}
