using Orleans;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Exceptions;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Storage;
using Vertex.Storage.EFCore.Storage;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Storage.Linq2db.Index;
using System.Collections.Generic;

namespace Vertex.Storage.Linq2db.Storage
{
    public class EventArchiveFactory : IEventArchiveFactory
    {
        readonly ConcurrentDictionary<Type, EventArchiveAttribute> typeAttributes = new ConcurrentDictionary<Type, EventArchiveAttribute>();
        readonly ConcurrentDictionary<string, bool> createTableDict = new ConcurrentDictionary<string, bool>();
        readonly ConcurrentDictionary<string, object> eventStorageDict = new ConcurrentDictionary<string, object>();
        readonly DbFactory dbFactory;
        readonly EventArchivePolicyContainer eventArchivePolicyContainer;
        readonly IGrainFactory grainFactory;
        readonly IServiceProvider serviceProvider;
        public EventArchiveFactory(
            IServiceProvider serviceProvider,
            IGrainFactory grainFactory,
            DbFactory dbFactory,
            EventArchivePolicyContainer eventArchivePolicyContainer)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
            this.eventArchivePolicyContainer = eventArchivePolicyContainer;
        }

        public ValueTask<IEventArchive<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor)
        {
            var attribute = typeAttributes.GetOrAdd(actor.GetType(), key =>
            {
                var attributes = key.GetCustomAttributes(typeof(EventArchiveAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes.First() as EventArchiveAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(EventArchiveAttribute)}=>{key.Name}");
                }
            });
            var storage = eventStorageDict.GetOrAdd($"{attribute.OptionName}_{attribute.Name}", key =>
            {
                if (!eventArchivePolicyContainer.Container.TryGetValue(attribute.Policy, out var policy))
                {
                    throw new KeyNotFoundException(attribute.Policy);
                }
                return new EventArchive<PrimaryKey>(serviceProvider, dbFactory, attribute.OptionName, async (long timestamp) =>
                {
                    var tableName = policy.Sharding(attribute.Name, timestamp).ToLower();
                    if (createTableDict.TryAdd($"{attribute.OptionName}_{tableName}", true))
                    {
                        using var db = this.dbFactory.GetEventDb(attribute.OptionName);
                        await db.CreateTableIfNotExists<EventEntity<PrimaryKey>>(this.grainFactory, key, tableName, async () =>
                        {
                            var indexGenerator = db.GetGenerator();
                            await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_unique", nameof(EventEntity<PrimaryKey>.ActorId), nameof(EventEntity<PrimaryKey>.Version));
                            await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_flow_unique", nameof(EventEntity<PrimaryKey>.ActorId), nameof(EventEntity<PrimaryKey>.Name), nameof(EventEntity<PrimaryKey>.FlowId));
                        });
                    }
                    return tableName;
                }, policy.Filter);
            });
            return ValueTask.FromResult(storage as IEventArchive<PrimaryKey>);
        }
    }
}
