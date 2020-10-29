using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SnapshotStorage<TPrimaryKey> : ISnapshotStorage<TPrimaryKey>
    {
        private readonly DbFactory dbFactory;
        private readonly string optionName;
        private readonly string tableName;
        private readonly ISerializer serializer;

        public SnapshotStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
            this.serializer = serviceProvider.GetService<ISerializer>();
        }

        public async Task Delete(TPrimaryKey actorId)
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
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
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        public async Task<SnapshotUnit<TPrimaryKey, T>> Get<T>(TPrimaryKey actorId)
            where T : ISnapshot, new()
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        return await db.Table<long>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => this.Convert<T>(o as SnapshotEntity<TPrimaryKey>)).SingleOrDefaultAsync();
                    };
                case string id:
                    {
                        var snapshotId = id;
                        return await db.Table<string>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => this.Convert<T>(o as SnapshotEntity<TPrimaryKey>)).SingleOrDefaultAsync();
                    };
                case Guid id:
                    {
                        var snapshotId = id;
                        return await db.Table<Guid>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => this.Convert<T>(o as SnapshotEntity<TPrimaryKey>)).SingleOrDefaultAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        private SnapshotUnit<TPrimaryKey, T> Convert<T>(SnapshotEntity<TPrimaryKey> snapshotEntity)
            where T : ISnapshot, new()
        {
            return new SnapshotUnit<TPrimaryKey, T>
            {
                Data = this.serializer.Deserialize<T>(snapshotEntity.Data),
                Meta = new SnapshotMeta<TPrimaryKey>
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

        public async Task Insert<T>(SnapshotUnit<TPrimaryKey, T> snapshot)
            where T : ISnapshot, new()
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
            await db.InsertAsync(new SnapshotEntity<TPrimaryKey>
            {
                Id = snapshot.Meta.ActorId,
                Data = this.serializer.Serialize(snapshot.Data),
                DoingVersion = snapshot.Meta.DoingVersion,
                Version = snapshot.Meta.Version,
                MinEventTimestamp = snapshot.Meta.MinEventTimestamp,
                MinEventVersion = snapshot.Meta.MinEventVersion,
                IsLatest = snapshot.Meta.IsLatest
            }, this.tableName);
        }

        public async Task Update<T>(SnapshotUnit<TPrimaryKey, T> snapshot)
            where T : ISnapshot, new()
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
            await db.UpdateAsync(new SnapshotEntity<TPrimaryKey>
            {
                Id = snapshot.Meta.ActorId,
                Data = this.serializer.Serialize(snapshot.Data),
                DoingVersion = snapshot.Meta.DoingVersion,
                Version = snapshot.Meta.Version,
                MinEventTimestamp = snapshot.Meta.MinEventTimestamp,
                MinEventVersion = snapshot.Meta.MinEventVersion,
                IsLatest = snapshot.Meta.IsLatest
            }, this.tableName);
        }

        public async Task UpdateIsLatest(TPrimaryKey actorId, bool isLatest)
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
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
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        public async Task UpdateMinEventTimestamp(TPrimaryKey actorId, long timestamp)
        {
            using var db = this.dbFactory.GetSnapshotDb(this.optionName);
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
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }
    }
}
