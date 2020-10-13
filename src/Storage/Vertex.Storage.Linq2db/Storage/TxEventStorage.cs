using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Transaction.Abstractions.Storage;
using Vertex.Utils.Channels;

namespace Vertex.Storage.EFCore.Storage
{
    public class TxEventStorage<PrimaryKey> : ITxEventStorage<PrimaryKey>
    {
        readonly DbFactory dbFactory;
        readonly string optionName, tableName;
        private readonly IMpscChannel<AskInputBox<EventEntity<PrimaryKey>, bool>> mpscChannel;
        private readonly ILogger<EventStorage<PrimaryKey>> logger;
        public TxEventStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
            this.logger = serviceProvider.GetService<ILogger<EventStorage<PrimaryKey>>>();
            this.mpscChannel = serviceProvider.GetService<IMpscChannel<AskInputBox<EventEntity<PrimaryKey>, bool>>>();
            this.mpscChannel.BindConsumer(this.BatchInsertExecuter);
        }
        #region private
        private async Task BatchInsertExecuter(List<AskInputBox<EventEntity<PrimaryKey>, bool>> inputList)
        {
            using var db = dbFactory.GetEventDb(optionName);
            var table = db.Table<PrimaryKey>().TableName(this.tableName);
            try
            {
                var copyResult = await table.BulkCopyAsync(inputList.Select(o => o.Value));
                if (copyResult.RowsCopied == inputList.Count)
                    inputList.ForEach(wrap => wrap.TaskSource.TrySetResult(true));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, ex.Message);
                foreach (var input in inputList)
                {
                    try
                    {
                        var result = await db.InsertAsync(input.Value, tableName);
                        input.TaskSource.TrySetResult(result > 0);
                    }
                    catch (Exception dbEx)
                    {
                        this.logger.LogError(dbEx, input.Value.Data);
                        var oldEvt = await table.Where(o => o.FlowId == input.Value.FlowId).FirstOrDefaultAsync();
                        if (oldEvt != default)
                        {
                            input.TaskSource.TrySetResult(false);
                        }
                        else
                        {
                            input.TaskSource.TrySetException(dbEx);
                        }
                    }
                }
            }
        }
        #endregion
        public async Task<bool> Append(EventDocument<PrimaryKey> document)
        {
            var entity = new EventEntity<PrimaryKey>
            {
                FlowId = document.FlowId,
                ActorId = document.ActorId,
                Data = document.Data,
                Name = document.Name,
                Timestamp = document.Timestamp,
                Version = document.Version
            };
            var wrap = new AskInputBox<EventEntity<PrimaryKey>, bool>(entity);
            await this.mpscChannel.WriteAsync(wrap);
            return await wrap.TaskSource.Task;
        }

        public async Task DeletePrevious(PrimaryKey actorId, long endVersion)
        {
            using var db = dbFactory.GetEventDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var paramId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        await table.Where(o => o.ActorId == paramId && o.Version <= endVersion).DeleteAsync();
                    }; break;
                case string id:
                    {
                        var paramId = id;
                        var table = db.Table<string>().TableName(this.tableName);
                        await table.Where(o => o.ActorId == paramId && o.Version <= endVersion).DeleteAsync();
                    }; break;
                case Guid id:
                    {
                        var paramId = id;
                        var table = db.Table<Guid>().TableName(this.tableName);
                        await table.Where(o => o.ActorId == paramId && o.Version <= endVersion).DeleteAsync();
                    }; break;
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }

        public async Task<IList<EventDocument<PrimaryKey>>> GetLatest(PrimaryKey actorId, int limit)
        {
            using var db = dbFactory.GetEventDb(optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var paramId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        return await table.Where(o => o.ActorId == paramId).OrderByDescending(o => o.Timestamp).Take(limit).Select(o =>
                          new EventDocument<PrimaryKey>
                          {
                              FlowId = o.FlowId,
                              ActorId = actorId,
                              Name = o.Name,
                              Data = o.Data,
                              Version = o.Version,
                              Timestamp = o.Timestamp
                          }).ToListAsync();
                    };
                case string id:
                    {
                        var paramId = id;
                        var table = db.Table<string>().TableName(this.tableName);
                        return await table.Where(o => o.ActorId == paramId).OrderByDescending(o => o.Timestamp).Take(limit).Select(o =>
                        new EventDocument<PrimaryKey>
                        {
                            FlowId = o.FlowId,
                            ActorId = actorId,
                            Name = o.Name,
                            Data = o.Data,
                            Version = o.Version,
                            Timestamp = o.Timestamp
                        }).ToListAsync();
                    };
                case Guid id:
                    {
                        var paramId = id;
                        var table = db.Table<Guid>().TableName(this.tableName);
                        return await table.Where(o => o.ActorId == paramId).OrderByDescending(o => o.Timestamp).Take(limit).Select(o =>
                        new EventDocument<PrimaryKey>
                        {
                            FlowId = o.FlowId,
                            ActorId = actorId,
                            Name = o.Name,
                            Data = o.Data,
                            Version = o.Version,
                            Timestamp = o.Timestamp
                        }).ToListAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            }
        }
    }
}
