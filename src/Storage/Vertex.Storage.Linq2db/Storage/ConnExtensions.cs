using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Orleans;
using Vertex.Abstractions.InnerService;

namespace Vertex.Storage.Linq2db.Storage
{
    public static class ConnExtensions
    {
        public static async Task CreateTableIfNotExists<TEntity>(this DataConnection conn, IGrainFactory grainFactory, string lockKey, string tableName, Func<Task> initFunc = default, int errorTimes = 0)
        {
            var lockService = grainFactory.GetGrain<ILockActor>(lockKey);
            if (await lockService.Lock(30 * 1000))
            {
                try
                {
                    var tables = await conn.GetTables();
                    if (!tables.Contains(tableName))
                    {
                        await conn.CreateTableAsync<TEntity>(tableName);
                    }
                    if (initFunc != default)
                    {
                        await initFunc();
                    }
                    await lockService.Unlock();
                }
                catch
                {
                    await lockService.Unlock();
                    if (errorTimes <= 3)
                    {
                        await CreateTableIfNotExists<TEntity>(conn, grainFactory, lockKey, tableName, initFunc, errorTimes++);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static async Task<List<string>> GetTables(this DataConnection conn)
        {
            if (conn.DataProvider.Name == DbProviderName.SQLite
                || conn.DataProvider.Name == DbProviderName.MSSQLite)
            {
                return await conn.QueryToListAsync<string>("SELECT name FROM sqlite_master WHERE type='table'");
            }
            else
            {
                var sp = conn.DataProvider.GetSchemaProvider();
                var dbSchema = sp.GetSchema(conn);
                return dbSchema.Tables.Select(t => t.TypeName).ToList();
            }
        }
    }
}
