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

namespace Vertex.Storage.Linq2db.Storage
{
    public class EventStorageFactory : IEventStorageFactory
    {
        readonly ConcurrentDictionary<Type, EventStorageAttribute> typeAttributes = new ConcurrentDictionary<Type, EventStorageAttribute>();
        readonly ConcurrentDictionary<string, Task<object>> eventStorageDict = new ConcurrentDictionary<string, Task<object>>();
        readonly DbFactory dbFactory;
        readonly IGrainFactory grainFactory;
        readonly IServiceProvider serviceProvider;
        public EventStorageFactory(IServiceProvider serviceProvider, DbFactory dbFactory, IGrainFactory grainFactory)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
        }
        public async ValueTask<IEventStorage<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor)
        {
            var attribute = typeAttributes.GetOrAdd(actor.GetType(), key =>
            {
                var attributes = key.GetCustomAttributes(typeof(EventStorageAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes.First() as EventStorageAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(EventStorageAttribute)}=>{key.Name}");
                }
            });
            var tableName = attribute.ShardingFunc(actor.ActorId.ToString());
            var storage = await eventStorageDict.GetOrAdd($"{attribute.OptionName}_{tableName}", async key =>
              {
                  using var db = this.dbFactory.GetEventDb(attribute.OptionName);
                  await db.CreateTableIfNotExists<EventEntity<PrimaryKey>>(this.grainFactory, key, tableName, async () =>
                  {
                      var indexGenerator = db.GetGenerator();
                      await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_unique", nameof(EventEntity<PrimaryKey>.ActorId), nameof(EventEntity<PrimaryKey>.Version));
                      await indexGenerator.CreateUniqueIndexIfNotExists(db, tableName, $"{tableName}_event_flow_unique", nameof(EventEntity<PrimaryKey>.ActorId), nameof(EventEntity<PrimaryKey>.Name), nameof(EventEntity<PrimaryKey>.FlowId));
                  });
                  return new EventStorage<PrimaryKey>(serviceProvider, dbFactory, attribute.OptionName, tableName);
              });
            return storage as IEventStorage<PrimaryKey>;
        }
    }
}
