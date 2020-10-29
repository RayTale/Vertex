using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Storage.Linq2db.Storage;

namespace Vertex.Storage.EFCore.Storage
{
    public class EventArchive<TPrimaryKey> : IEventArchive<TPrimaryKey>
    {
        private readonly DbFactory dbFactory;
        private readonly string optionName;
        private readonly ILogger<EventStorage<TPrimaryKey>> logger;
        private readonly Func<long, ValueTask<string>> tableFunc;
        private readonly Func<string, bool> tableFilter;

        public EventArchive(
            IServiceProvider serviceProvider,
            DbFactory dbFactory,
            string optionName,
            Func<long, ValueTask<string>> tableFunc,
            Func<string, bool> tableFilter)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableFunc = tableFunc;
            this.tableFilter = tableFilter;
            this.logger = serviceProvider.GetService<ILogger<EventStorage<TPrimaryKey>>>();
        }

        public async Task Arichive(IList<EventDocument<TPrimaryKey>> documents)
        {
            var groups = new Dictionary<string, List<EventDocument<TPrimaryKey>>>();
            foreach (var doc in documents)
            {
                var key = await this.tableFunc(doc.Timestamp);
                if (groups.ContainsKey(key))
                {
                    groups[key].Add(doc);
                }
                else
                {
                    groups[key] = new List<EventDocument<TPrimaryKey>> { doc };
                }
            }

            using var db = this.dbFactory.GetEventDb(this.optionName);
            foreach (var group in groups)
            {
                var inputList = group.Value.Select(document => new EventEntity<TPrimaryKey>
                {
                    FlowId = document.FlowId,
                    ActorId = document.ActorId,
                    Data = document.Data,
                    Name = document.Name,
                    Timestamp = document.Timestamp,
                    Version = document.Version,
                }).ToList();
                var table = db.Table<TPrimaryKey>().TableName(group.Key);
                try
                {
                    await table.BulkCopyAsync(inputList);
                }
                catch
                {
                    foreach (var input in inputList)
                    {
                        try
                        {
                            await db.InsertAsync(input, group.Key);
                        }
                        catch (Exception dbEx)
                        {
                            this.logger.LogError(dbEx, input.Data);
                            var exist = await Exist(db, group.Key, input);
                            if (!exist)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public async Task<List<EventDocument<TPrimaryKey>>> GetList(TPrimaryKey actorId, long startVersion, long endVersion)
        {
            using var db = this.dbFactory.GetEventDb(this.optionName);
            var tables = (await db.GetTables()).Where(this.tableFilter).ToList();
            tables.Reverse();
            var capacity = (int)(endVersion - startVersion);
            var result = new List<EventDocument<TPrimaryKey>>(capacity);
            switch (actorId)
            {
                case long id:
                    {
                        var paramId = id;
                        foreach (var tableName in tables)
                        {
                            var table = db.Table<long>().TableName(tableName);
                            var documents = await table.Where(o => o.ActorId == paramId && o.Version >= startVersion && o.Version <= endVersion).OrderBy(o => o.Version).Select(o =>
                               new EventDocument<TPrimaryKey>
                               {
                                   FlowId = o.FlowId,
                                   ActorId = actorId,
                                   Name = o.Name,
                                   Data = o.Data,
                                   Version = o.Version,
                                   Timestamp = o.Timestamp,
                               }).ToListAsync();
                            if (documents.Count > 0)
                            {
                                result.AddRange(documents);
                                if (result.Count >= capacity)
                                {
                                    break;
                                }
                            }
                        }

                        return result;
                    };
                case string id:
                    {
                        var paramId = id;
                        foreach (var tableName in tables)
                        {
                            var table = db.Table<string>().TableName(tableName);
                            var documents = await table.Where(o => o.ActorId == paramId && o.Version >= startVersion && o.Version <= endVersion).OrderBy(o => o.Version).Select(o =>
                               new EventDocument<TPrimaryKey>
                               {
                                   FlowId = o.FlowId,
                                   ActorId = actorId,
                                   Name = o.Name,
                                   Data = o.Data,
                                   Version = o.Version,
                                   Timestamp = o.Timestamp
                               }).ToListAsync();
                            if (documents.Count > 0)
                            {
                                result.AddRange(documents);
                                if (result.Count >= capacity)
                                {
                                    break;
                                }
                            }
                        }

                        return result;
                    };
                case Guid id:
                    {
                        var paramId = id;
                        foreach (var tableName in tables)
                        {
                            var table = db.Table<Guid>().TableName(tableName);
                            var documents = await table.Where(o => o.ActorId == paramId && o.Version >= startVersion && o.Version <= endVersion).OrderBy(o => o.Version).Select(o =>
                               new EventDocument<TPrimaryKey>
                               {
                                   FlowId = o.FlowId,
                                   ActorId = actorId,
                                   Name = o.Name,
                                   Data = o.Data,
                                   Version = o.Version,
                                   Timestamp = o.Timestamp
                               }).ToListAsync();
                            if (documents.Count > 0)
                            {
                                result.AddRange(documents);
                                if (result.Count >= capacity)
                                {
                                    break;
                                }
                            }
                        }

                        return result;
                    };
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        private static Task<bool> Exist(EventDb db, string tableName, EventEntity<TPrimaryKey> entity)
        {
            switch (entity.ActorId)
            {
                case long id:
                    {
                        var paramId = id;
                        var table = db.Table<long>().TableName(tableName);
                        return table.Where(o => o.ActorId == paramId && o.Name == entity.Name && o.FlowId == entity.FlowId).AnyAsync();
                    };
                case string id:
                    {
                        var paramId = id;
                        var table = db.Table<string>().TableName(tableName);
                        return table.Where(o => o.ActorId == paramId && o.Name == entity.Name && o.FlowId == entity.FlowId).AnyAsync();
                    };
                case Guid id:
                    {
                        var paramId = id;
                        var table = db.Table<Guid>().TableName(tableName);
                        return table.Where(o => o.ActorId == paramId && o.Name == entity.Name && o.FlowId == entity.FlowId).AnyAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }
    }
}
