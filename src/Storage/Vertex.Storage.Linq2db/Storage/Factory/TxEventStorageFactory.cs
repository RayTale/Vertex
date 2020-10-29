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
        private readonly ConcurrentDictionary<Type, TxEventStorageAttribute> typeAttributes = new ConcurrentDictionary<Type, TxEventStorageAttribute>();
        private readonly ConcurrentDictionary<string, Task<object>> eventStorageDict = new ConcurrentDictionary<string, Task<object>>();
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
                var attributes = key.GetCustomAttributes(typeof(TxEventStorageAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes.First() as TxEventStorageAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(TxEventStorageAttribute)}=>{key.Name}");
                }
            });
            var tableName = attribute.ShardingFunc(actor.ActorId.ToString());
            var storage = await this.eventStorageDict.GetOrAdd($"{attribute.OptionName}_{tableName}", async key =>
             {
                 using var db = this.dbFactory.GetEventDb(attribute.OptionName);
                 await db.CreateTableIfNotExists<EventEntity<TPrimaryKey>>(this.grainFactory, key, tableName, async () =>
                 {
                     var indexGenerator = db.GetGenerator();
                     await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Version).ToLower());
                     await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_flow_unique", nameof(EventEntity<TPrimaryKey>.ActorId).ToLower(), nameof(EventEntity<TPrimaryKey>.Name).ToLower(), nameof(EventEntity<TPrimaryKey>.FlowId).ToLower());
                 });
                 return new TxEventStorage<TPrimaryKey>(this.serviceProvider, this.dbFactory, attribute.OptionName, tableName);
             });

            return storage as ITxEventStorage<TPrimaryKey>;
        }
    }
}
