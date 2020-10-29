using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Storage
{
    public class SubSnapshotStorage<TPrimaryKey> : ISubSnapshotStorage<TPrimaryKey>
    {
        private readonly DbFactory dbFactory;
        private readonly string optionName;
        private readonly string tableName;

        public SubSnapshotStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
        }

        public async Task Delete(TPrimaryKey actorId)
        {
            using var db = this.dbFactory.GetSubSnapshotDb(this.optionName);
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

        public async Task<SubSnapshot<TPrimaryKey>> Get(TPrimaryKey actorId)
        {
            using var db = this.dbFactory.GetSubSnapshotDb(this.optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var snapshotId = id;
                        return await db.Table<long>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<TPrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                case string id:
                    {
                        var snapshotId = id;
                        return await db.Table<string>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<TPrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                case Guid id:
                    {
                        var snapshotId = id;
                        return await db.Table<Guid>().TableName(this.tableName).Where(o => o.Id == snapshotId).Select(o => new SubSnapshot<TPrimaryKey> { ActorId = actorId, DoingVersion = o.DoingVersion, Version = o.Version }).SingleOrDefaultAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        public async Task Insert(SubSnapshot<TPrimaryKey> snapshot)
        {
            using var db = this.dbFactory.GetSubSnapshotDb(this.optionName);
            await db.InsertAsync(new SubSnapshotEntity<TPrimaryKey>
            {
                Id = snapshot.ActorId,
                DoingVersion = snapshot.DoingVersion,
                Version = snapshot.Version
            }, this.tableName);
        }

        public async Task Update(SubSnapshot<TPrimaryKey> snapshot)
        {
            using var db = this.dbFactory.GetSubSnapshotDb(this.optionName);
            await db.UpdateAsync(new SubSnapshotEntity<TPrimaryKey>
            {
                Id = snapshot.ActorId,
                DoingVersion = snapshot.DoingVersion,
                Version = snapshot.Version
            }, this.tableName);
        }
    }
}
