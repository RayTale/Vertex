using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Storage.Linq2db.Storage;

namespace Vertex.Storage.EFCore.Storage
{
    public class EventArchive<PrimaryKey> : IEventArchive<PrimaryKey>
    {
        readonly DbFactory dbFactory;
        readonly string optionName;
        private readonly ILogger<EventStorage<PrimaryKey>> logger;
        private readonly Func<long, ValueTask<string>> tableFunc;
        private readonly Func<string, bool> tableFilter;
        public EventArchive(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, Func<long, ValueTask<string>> tableFunc, Func<string, bool> tableFilter)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableFunc = tableFunc;
            this.tableFilter = tableFilter;
            this.logger = serviceProvider.GetService<ILogger<EventStorage<PrimaryKey>>>();
        }

        public async Task Arichive(IList<EventDocument<PrimaryKey>> documents)
        {
            var groups = new Dictionary<string, List<EventDocument<PrimaryKey>>>();
            foreach (var doc in documents)
            {
                var key = await this.tableFunc(doc.Timestamp);
                if (groups.ContainsKey(key))
                {
                    groups[key].Add(doc);
                }
                else
                {
                    groups[key] = new List<EventDocument<PrimaryKey>> { doc };
                }
            }
            using var db = dbFactory.GetEventDb(optionName);
            foreach (var group in groups)
            {
                var inputList = group.Value.Select(document => new EventEntity<PrimaryKey>
                {
                    FlowId = document.FlowId,
                    ActorId = document.ActorId,
                    Data = document.Data,
                    Name = document.Name,
                    Timestamp = document.Timestamp,
                    Version = document.Version
                }).ToList();
                var table = db.Table<PrimaryKey>().TableName(group.Key);
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
        public async Task<List<EventDocument<PrimaryKey>>> GetList(PrimaryKey actorId, long startVersion, long endVersion)
        {
            using var db = dbFactory.GetEventDb(optionName);
            var tables = (await db.GetTables()).Where(tableFilter).ToList();
            tables.Reverse();
            var capacity = (int)(endVersion - startVersion);
            var result = new List<EventDocument<PrimaryKey>>(capacity);
            switch (actorId)
            {
                case long id:
                    {
                        var paramId = id;
                        foreach (var tableName in tables)
                        {
                            var table = db.Table<long>().TableName(tableName);
                            var documents = await table.Where(o => o.ActorId == paramId && o.Version >= startVersion && o.Version <= endVersion).OrderBy(o => o.Version).Select(o =>
                               new EventDocument<PrimaryKey>
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
                                    break;
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
                               new EventDocument<PrimaryKey>
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
                                    break;
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
                               new EventDocument<PrimaryKey>
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
                                    break;
                            }
                        }
                        return result;
                    };
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }
        private static Task<bool> Exist(EventDb db, string tableName, EventEntity<PrimaryKey> entity)
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
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }
    }
}
