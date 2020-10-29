using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.EventStream;
using Vertex.Protocol;

namespace Vertex.Stream.Common
{
    public class StreamSubHandler : IStreamSubHandler
    {
        private static readonly ConcurrentDictionary<Type, object> ObserverGeneratorDict = new ConcurrentDictionary<Type, object>();

        private readonly IGrainFactory clusterClient;

        public StreamSubHandler(IGrainFactory grainFactory, ILogger<StreamSubHandler> logger)
        {
            this.clusterClient = grainFactory;
            this.Logger = logger;
        }

        protected ILogger Logger { get; private set; }

        public async Task EventHandler(Type observerType, BytesBox bytes)
        {
            if (EventConverter.TryParseActorId(bytes.Value, out var actorId))
            {
                switch (actorId)
                {
                    case long id: await this.GetObserver(observerType, id).OnNext(new Immutable<byte[]>(bytes.Value)); break;
                    case string id: await this.GetObserver(observerType, id).OnNext(new Immutable<byte[]>(bytes.Value)); break;
                    case Guid id: await this.GetObserver(observerType, id).OnNext(new Immutable<byte[]>(bytes.Value)); break;
                    default: break;
                }
                bytes.Success = true;
            }
            else
            {
                if (this.Logger.IsEnabled(LogLevel.Error))
                {
                    this.Logger.LogError($"{nameof(EventConverter.TryParseActorId)} failed");
                }
            }
        }

        public Task EventHandler(Type observerType, List<BytesBox> list)
        {
            var groups = list.Select(bytes =>
            {
                var success = EventConverter.TryParseActorId(bytes.Value, out var actorId);
                if (!success)
                {
                    if (this.Logger.IsEnabled(LogLevel.Error))
                    {
                        this.Logger.LogError($"{nameof(EventConverter.TryParseActorId)} failed");
                    }
                }

                return (success, actorId, bytes);
            }).Where(o => o.success).GroupBy(o => o.actorId);
            return Task.WhenAll(groups.Select(async kv =>
            {
                var items = kv.Select(item => item.bytes.Value).ToList();
                switch (kv.Key)
                {
                    case long id: await this.GetObserver(observerType, id).OnNext(new Immutable<List<byte[]>>(items)); break;
                    case string id: await this.GetObserver(observerType, id).OnNext(new Immutable<List<byte[]>>(items)); break;
                    case Guid id: await this.GetObserver(observerType, id).OnNext(new Immutable<List<byte[]>>(items)); break;
                    default: break;
                }
                foreach (var (_, _, bytes) in kv)
                {
                    bytes.Success = true;
                }
            }));
        }

        private IStreamHandler GetObserver<TPrimaryKey>(Type observerType, TPrimaryKey primaryKey)
        {
            var func = ObserverGeneratorDict.GetOrAdd(observerType, key =>
            {
                var clientType = typeof(IGrainFactory);
                var clientParams = Expression.Parameter(clientType, "client");
                var primaryKeyParams = Expression.Parameter(typeof(TPrimaryKey), "primaryKey");
                var grainClassNamePrefixParams = Expression.Parameter(typeof(string), "grainClassNamePrefix");
                var method = typeof(ClusterClientExtensions).GetMethod("GetGrain", new Type[] { clientType, typeof(TPrimaryKey), typeof(string) });
                var body = Expression.Call(method.MakeGenericMethod(observerType), clientParams, primaryKeyParams, grainClassNamePrefixParams);
                return Expression.Lambda<Func<IGrainFactory, TPrimaryKey, string, IStreamHandler>>(body, clientParams, primaryKeyParams, grainClassNamePrefixParams).Compile();
            }) as Func<IGrainFactory, TPrimaryKey, string, IStreamHandler>;
            return func(this.clusterClient, primaryKey, null);
        }
    }
}
