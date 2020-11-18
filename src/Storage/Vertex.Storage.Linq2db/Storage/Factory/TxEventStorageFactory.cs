using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Exceptions;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Storage.Linq2db.Index;
using Vertex.Transaction.Abstractions.Storage;

namespace Vertex.Storage.Linq2db.Storage
{
    public class TxEventStorageFactory : ITxEventStorageFactory
    {
        private readonly ConcurrentDictionary<Type, TxEventStorageBaseAttribute> typeAttributes = new();
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> eventStorageDict = new();
        private readonly DbFactory dbFactory;
        private readonly IGrainFactory grainFactory;
        private readonly IServiceProvider serviceProvider;

        public TxEventStorageFactory(IServiceProvider serviceProvider, DbFactory dbFactory, IGrainFactory grainFactory)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
        }

        public async ValueTask<ITxEventStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor)
        {
            var attribute = this.typeAttributes.GetOrAdd(actor.GetType(), key =>
            {
                var attributes = key.GetCustomAttributes(false);
                var attribute = attributes.SingleOrDefault(att => typeof(TxEventStorageBaseAttribute).IsAssignableFrom(att.GetType()));
                if (attribute != default)
                {
                    return attribute as TxEventStorageBaseAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(TxEventStorageBaseAttribute)}=>{key.Name}");
                }
            });
            var actorId = actor.ActorId.ToString();
            var tableName = attribute.GetTableName(actorId);
            var optionName = attribute.GetOptionName(actorId);
            var storage = await this.eventStorageDict.GetOrAdd($"{optionName}_{tableName}", key =>
            new Lazy<Task<object>>(async () =>
            {
                using var db = this.dbFactory.GetEventDb(optionName);
                await db.CreateTableIfNotExists<EventEntity<TPrimaryKey>>(this.grainFactory, key, tableName, async () =>
                {
                    var indexGenerator = db.GetGenerator();
                    await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Version).ToLower());
                    await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_flow_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Name).ToLower(), nameof(EventEntity<TPrimaryKey>.FlowId).ToLower());
                });
                return new TxEventStorage<TPrimaryKey>(this.serviceProvider, this.dbFactory, optionName, tableName);
            })).Value;

            return storage as ITxEventStorage<TPrimaryKey>;
        }
    }
}
