using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Storage.EFCore.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SnapshotStorage<PrimaryKey> : ISnapshotStorage<PrimaryKey>
    {
        private readonly DbFactory dbFactory;
        private readonly string optionName, tableName;
        private readonly ILogger<EventStorage<PrimaryKey>> logger;
        private readonly ISerializer serializer;
        public SnapshotStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
            this.logger = serviceProvider.GetService<ILogger<EventStorage<PrimaryKey>>>();
            this.serializer = serviceProvider.GetService<ISerializer>();
        }

        public async Task Delete(PrimaryKey actorId)
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).DeleteAsync();
                    }; break;
                case string id:
                    {
                        var snapshotId = id;
                        var table = db.Table<string>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).DeleteAsync();
                    }; break;
                case Guid id:
                    {
                        var snapshotId = id;
                        var table = db.Table<Guid>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).DeleteAsync();
                    }; break;
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }

        public async Task<SnapshotUnit<PrimaryKey, T>> Get<T>(PrimaryKey actorId) where T : ISnapshot, new()
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        return await db.Table<long>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => Convert<T>(o as SnapshotEntity<PrimaryKey>)).SingleOrDefaultAsync();
                    };
                case string id:
                    {
                        var snapshotId = id;
                        return await db.Table<string>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => Convert<T>(o as SnapshotEntity<PrimaryKey>)).SingleOrDefaultAsync();
                    };
                case Guid id:
                    {
                        var snapshotId = id;
                        return await db.Table<Guid>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => Convert<T>(o as SnapshotEntity<PrimaryKey>)).SingleOrDefaultAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }
        private SnapshotUnit<PrimaryKey, T> Convert<T>(SnapshotEntity<PrimaryKey> snapshotEntity) where T : ISnapshot, new()
        {
            return new SnapshotUnit<PrimaryKey, T>
            {
                Data = serializer.Deserialize<T>(snapshotEntity.Data),
                Meta = new SnapshotMeta<PrimaryKey>
                {
                    ActorId = snapshotEntity.Id,
                    DoingVersion = snapshotEntity.DoingVersion,
                    Version = snapshotEntity.Version,
                    MinEventTimestamp = snapshotEntity.MinEventTimestamp,
                    MinEventVersion = snapshotEntity.MinEventVersion,
                    IsLatest = snapshotEntity.IsLatest
                }
            };
        }
        public async Task Insert<T>(SnapshotUnit<PrimaryKey, T> snapshot) where T : ISnapshot, new()
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            await db.InsertAsync(new SnapshotEntity<PrimaryKey>
            {
                Id = snapshot.Meta.ActorId,
                Data = serializer.Serialize(snapshot.Data),
                DoingVersion = snapshot.Meta.DoingVersion,
                Version = snapshot.Meta.Version,
                MinEventTimestamp = snapshot.Meta.MinEventTimestamp,
                MinEventVersion = snapshot.Meta.MinEventVersion,
                IsLatest = snapshot.Meta.IsLatest
            }, this.tableName);
        }

        public async Task Update<T>(SnapshotUnit<PrimaryKey, T> snapshot) where T : ISnapshot, new()
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            await db.UpdateAsync(new SnapshotEntity<PrimaryKey>
            {
                Id = snapshot.Meta.ActorId,
                Data = serializer.Serialize(snapshot.Data),
                DoingVersion = snapshot.Meta.DoingVersion,
                Version = snapshot.Meta.Version,
                MinEventTimestamp = snapshot.Meta.MinEventTimestamp,
                MinEventVersion = snapshot.Meta.MinEventVersion,
                IsLatest = snapshot.Meta.IsLatest
            }, this.tableName);
        }

        public async Task UpdateIsLatest(PrimaryKey actorId, bool isLatest)
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.IsLatest, isLatest).UpdateAsync();
                    }; break;
                case string id:
                    {
                        var snapshotId = id;
                        var table = db.Table<string>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.IsLatest, isLatest).UpdateAsync();
                    }; break;
                case Guid id:
                    {
                        var snapshotId = id;
                        var table = db.Table<Guid>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.IsLatest, isLatest).UpdateAsync();
                    }; break;
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }

        public async Task UpdateMinEventTimestamp(PrimaryKey actorId, long timestamp)
        {
            using var db = dbFactory.GetSnapshotDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.MinEventTimestamp, timestamp).UpdateAsync();
                    }; break;
                case string id:
                    {
                        var snapshotId = id;
                        var table = db.Table<string>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.MinEventTimestamp, timestamp).UpdateAsync();
                    }; break;
                case Guid id:
                    {
                        var snapshotId = id;
                        var table = db.Table<Guid>().TableName(this.tableName);
                        await table.Where(o => o.Id == snapshotId).Set(o => o.MinEventTimestamp, timestamp).UpdateAsync();
                    }; break;
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }
    }
}
