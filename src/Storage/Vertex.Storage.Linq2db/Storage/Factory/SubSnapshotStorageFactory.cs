using Orleans;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Exceptions;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entitys;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SubSnapshotStorageFactory : ISubSnapshotStorageFactory
    {
        readonly ConcurrentDictionary<Type, SnapshotStorageAttribute> typeAttributes = new ConcurrentDictionary<Type, SnapshotStorageAttribute>();
        readonly ConcurrentDictionary<string, Task<object>> eventStorageDict = new ConcurrentDictionary<string, Task<object>>();
        readonly DbFactory dbFactory;
        readonly IGrainFactory grainFactory;
        readonly IServiceProvider serviceProvider;
        public SubSnapshotStorageFactory(IServiceProvider serviceProvider, DbFactory dbFactory, IGrainFactory grainFactory)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
        }
        public async ValueTask<ISubSnapshotStorage<PrimaryKey>> Create<PrimaryKey>(IActor<PrimaryKey> actor)
        {
            var attribute = typeAttributes.GetOrAdd(actor.GetType(), key =>
            {
                var attributes = key.GetCustomAttributes(typeof(SnapshotStorageAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes.First() as SnapshotStorageAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(SnapshotStorageAttribute)}=>{key.Name}");
                }
            });
            var tableName = attribute.ShardingFunc(actor.ActorId.ToString());
            var storage = await eventStorageDict.GetOrAdd($"{attribute.OptionName}_{tableName}", async key =>
              {
                  using var db = this.dbFactory.GetEventDb(attribute.OptionName);
                  await db.CreateTableIfNotExists<SubSnapshotEntity<PrimaryKey>>(this.grainFactory, key, tableName);
                  return new SubSnapshotStorage<PrimaryKey>(serviceProvider, dbFactory, attribute.OptionName, tableName);
              });

            return storage as ISubSnapshotStorage<PrimaryKey>;
        }
    }
}
