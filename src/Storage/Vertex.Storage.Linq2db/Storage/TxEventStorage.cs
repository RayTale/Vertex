using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vertex.Abstractions.Event;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Entities;
using Vertex.Transaction.Abstractions.Storage;
using Vertex.Utils.Channels;

namespace Vertex.Storage.Linq2db.Storage
{
    public class TxEventStorage<TPrimaryKey> : ITxEventStorage<TPrimaryKey>
    {
        private readonly DbFactory dbFactory;
        private readonly string optionName;
        private readonly string tableName;
        private readonly IMpscChannel<AskInputBox<EventEntity<TPrimaryKey>, bool>> mpscChannel;
        private readonly ILogger<TxEventStorage<TPrimaryKey>> logger;

        public TxEventStorage(IServiceProvider serviceProvider, DbFactory dbFactory, string optionName, string tableName)
        {
            this.dbFactory = dbFactory;
            this.optionName = optionName;
            this.tableName = tableName;
            this.logger = serviceProvider.GetService<ILogger<TxEventStorage<TPrimaryKey>>>();
            this.mpscChannel = serviceProvider.GetService<IMpscChannel<AskInputBox<EventEntity<TPrimaryKey>, bool>>>();
            this.mpscChannel.BindConsumer(this.BatchInsertExecuter);
        }

        #region private
        private async Task BatchInsertExecuter(List<AskInputBox<EventEntity<TPrimaryKey>, bool>> inputList)
        {
            using var db = this.dbFactory.GetEventDb(this.optionName);
            var table = db.Table<TPrimaryKey>().TableName(this.tableName);
            try
            {
                var copyResult = await table.BulkCopyAsync(inputList.Select(o => o.Value));
                if (copyResult.RowsCopied == inputList.Count)
                {
                    inputList.ForEach(wrap => wrap.TaskSource.TrySetResult(true));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, ex.Message);
                foreach (var input in inputList)
                {
                    try
                    {
                        var result = await db.InsertAsync(input.Value, this.tableName);
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

        public async Task<bool> Append(EventDocument<TPrimaryKey> document)
        {
            var entity = new EventEntity<TPrimaryKey>
            {
                FlowId = document.FlowId,
                ActorId = document.ActorId,
                Data = document.Data,
                Name = document.Name,
                Timestamp = document.Timestamp,
                Version = document.Version
            };
            var wrap = new AskInputBox<EventEntity<TPrimaryKey>, bool>(entity);
            await this.mpscChannel.WriteAsync(wrap);
            return await wrap.TaskSource.Task;
        }

        public async Task DeletePrevious(TPrimaryKey actorId, long endVersion)
        {
            using var db = this.dbFactory.GetEventDb(this.optionName);
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
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }

        public async Task<IList<EventDocument<TPrimaryKey>>> GetLatest(TPrimaryKey actorId, int limit)
        {
            using var db = this.dbFactory.GetEventDb(this.optionName);
            switch (actorId)
            {
                case long id:
                    {
                        var paramId = id;
                        var table = db.Table<long>().TableName(this.tableName);
                        return await table.Where(o => o.ActorId == paramId).OrderByDescending(o => o.Timestamp).Take(limit).Select(o =>
                          new EventDocument<TPrimaryKey>
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
                        new EventDocument<TPrimaryKey>
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
                        new EventDocument<TPrimaryKey>
                        {
                            FlowId = o.FlowId,
                            ActorId = actorId,
                            Name = o.Name,
                            Data = o.Data,
                            Version = o.Version,
                            Timestamp = o.Timestamp
                        }).ToListAsync();
                    };
                default: throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }
        }
    }
}
