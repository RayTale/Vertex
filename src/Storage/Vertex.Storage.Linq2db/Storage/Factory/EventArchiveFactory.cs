using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Exceptions;
using Vertex.Abstractions.Storage;
using Vertex.Storage.EFCore.Storage;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Storage.Linq2db.Index;

namespace Vertex.Storage.Linq2db.Storage
{
    public class EventArchiveFactory : IEventArchiveFactory
    {
        private readonly ConcurrentDictionary<Type, EventArchiveAttribute> typeAttributes = new ConcurrentDictionary<Type, EventArchiveAttribute>();
        private readonly ConcurrentDictionary<string, bool> createTableDict = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<string, object> eventStorageDict = new ConcurrentDictionary<string, object>();
        private readonly DbFactory dbFactory;
        private readonly EventArchivePolicyContainer eventArchivePolicyContainer;
        private readonly IGrainFactory grainFactory;
        private readonly IServiceProvider serviceProvider;

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

        public ValueTask<IEventArchive<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor)
        {
            var attribute = this.typeAttributes.GetOrAdd(actor.GetType(), key =>
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
            var storage = this.eventStorageDict.GetOrAdd($"{attribute.OptionName}_{attribute.Name}", key =>
            {
                if (!this.eventArchivePolicyContainer.Container.TryGetValue(attribute.Policy, out var policy))
                {
                    throw new KeyNotFoundException(attribute.Policy);
                }
                return new EventArchive<TPrimaryKey>(this.serviceProvider, this.dbFactory, attribute.OptionName, async (long timestamp) =>
                {
                    var tableName = policy.Sharding(attribute.Name, timestamp).ToLower();
                    if (this.createTableDict.TryAdd($"{attribute.OptionName}_{tableName}", true))
                    {
                        using var db = this.dbFactory.GetEventDb(attribute.OptionName);
                        await db.CreateTableIfNotExists<EventEntity<TPrimaryKey>>(this.grainFactory, key, tableName, async () =>
                        {
                            var indexGenerator = db.GetGenerator();
                            await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Version).ToLower());
                            await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_flow_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Name).ToLower(), nameof(EventEntity<TPrimaryKey>.FlowId).ToLower());
                        });
                    }
                    return tableName;
                }, policy.Filter);
            });
            return ValueTask.FromResult(storage as IEventArchive<TPrimaryKey>);
        }
    }
}
