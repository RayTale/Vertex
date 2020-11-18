using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Exceptions;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SubSnapshotStorageFactory : ISubSnapshotStorageFactory
    {
        private readonly ConcurrentDictionary<Type, SnapshotStorageBaseAttribute> typeAttributes = new();
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> eventStorageDict = new();
        private readonly DbFactory dbFactory;
        private readonly IGrainFactory grainFactory;
        private readonly IServiceProvider serviceProvider;

        public SubSnapshotStorageFactory(IServiceProvider serviceProvider, DbFactory dbFactory, IGrainFactory grainFactory)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
        }

        public async ValueTask<ISubSnapshotStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor)
        {
            var attribute = this.typeAttributes.GetOrAdd(actor.GetType(), key =>
            {
                var attributes = key.GetCustomAttributes(false);
                var attribute = attributes.SingleOrDefault(att => typeof(SnapshotStorageBaseAttribute).IsAssignableFrom(att.GetType()));
                if (attribute != default)
                {
                    return attribute as SnapshotStorageBaseAttribute;
                }
                else
                {
                    throw new MissingAttributeException($"{nameof(SnapshotStorageBaseAttribute)}=>{key.Name}");
                }
            });
            var actorId = actor.ActorId.ToString();
            var tableName = attribute.GetTableName(actorId);
            var optionName = attribute.GetOptionName(actorId);
            var storage = await this.eventStorageDict.GetOrAdd($"{optionName}_{tableName}", key =>
             new Lazy<Task<object>>(async () =>
             {
                 using var db = this.dbFactory.GetEventDb(optionName);
                 await db.CreateTableIfNotExists<SubSnapshotEntity<TPrimaryKey>>(this.grainFactory, key, tableName);
                 return new SubSnapshotStorage<TPrimaryKey>(this.serviceProvider, this.dbFactory, optionName, tableName);
             })).Value;

            return storage as ISubSnapshotStorage<TPrimaryKey>;
        }
    }
}
