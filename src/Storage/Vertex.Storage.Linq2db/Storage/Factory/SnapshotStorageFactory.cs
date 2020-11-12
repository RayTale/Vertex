﻿using System;
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
    public class SnapshotStorageFactory : ISnapshotStorageFactory
    {
        private readonly ConcurrentDictionary<Type, SnapshotStorageAttribute> typeAttributes = new ConcurrentDictionary<Type, SnapshotStorageAttribute>();
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> eventStorageDict = new ConcurrentDictionary<string, Lazy<Task<object>>>();
        private readonly DbFactory dbFactory;
        private readonly IGrainFactory grainFactory;
        private readonly IServiceProvider serviceProvider;

        public SnapshotStorageFactory(IServiceProvider serviceProvider, DbFactory dbFactory, IGrainFactory grainFactory)
        {
            this.dbFactory = dbFactory;
            this.grainFactory = grainFactory;
            this.serviceProvider = serviceProvider;
        }

        public async ValueTask<ISnapshotStorage<TPrimaryKey>> Create<TPrimaryKey>(IActor<TPrimaryKey> actor)
        {
            var attribute = this.typeAttributes.GetOrAdd(actor.GetType(), key =>
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
            var storage = await this.eventStorageDict.GetOrAdd($"{attribute.OptionName}_{tableName}", key =>
            new Lazy<Task<object>>(async () =>
           {
               using var db = this.dbFactory.GetEventDb(attribute.OptionName);
               await db.CreateTableIfNotExists<SnapshotEntity<TPrimaryKey>>(this.grainFactory, key, tableName);
               return new SnapshotStorage<TPrimaryKey>(this.serviceProvider, this.dbFactory, attribute.OptionName, tableName);
           })).Value;

            return storage as ISnapshotStorage<TPrimaryKey>;
        }
    }
}
