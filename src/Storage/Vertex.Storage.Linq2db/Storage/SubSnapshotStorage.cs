using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Storage.EFCore.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SubSnapshotStorage<PrimaryKey> : ISubSnapshotStorage<PrimaryKey>
    {
        readonly DbFactory dbFactory;
        readonly string optionName, tableName;
        private readonly ILogger<EventStorage<PrimaryKey>> logger;
        public SubSnapshotStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
            this.logger = serviceProvider.GetService<ILogger<EventStorage<PrimaryKey>>>();
        }

        public async Task Delete(PrimaryKey actorId)
        {
            using var db = dbFactory.GetSubSnapshotDb(optionName);
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

        public async Task<SubSnapshot<PrimaryKey>> Get(PrimaryKey actorId)
        {
            using var db = dbFactory.GetSubSnapshotDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        return await db.Table<long>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<PrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                case string id:
                    {
                        var snapshotId = id;
                        return await db.Table<string>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<PrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                case Guid id:
                    {
                        var snapshotId = id;
                        return await db.Table<Guid>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<PrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }

        public async Task Insert(SubSnapshot<PrimaryKey> snapshot)
        {
            using var db = dbFactory.GetSubSnapshotDb(optionName);
            await db.InsertAsync(new SubSnapshotEntity<PrimaryKey>
            {
                Id = snapshot.ActorId,
                DoingVersion = snapshot.DoingVersion,
                Version = snapshot.Version
            }, this.tableName);
        }

        public async Task Update(SubSnapshot<PrimaryKey> snapshot)
        {
            using var db = dbFactory.GetSubSnapshotDb(optionName);
            await db.UpdateAsync(new SubSnapshotEntity<PrimaryKey>
            {
                Id = snapshot.ActorId,
                DoingVersion = snapshot.DoingVersion,
                Version = snapshot.Version
            }, this.tableName);
        }
    }
}
