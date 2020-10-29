using System.Threading.Tasks;
using LinqToDB.Data;

namespace Vertex.Storage.Linq2db.Index.MySql
{
    public class MySqlIndexGenerator : IIndexGenerator
    {
        public async Task CreateIndexIfNotExists(DataConnection conn, string table, string indexName, params string[] columns)
        {
            indexName = indexName.Replace('.', '_');
            var indexList = await conn.QueryToListAsync<string>("SELECT INDEX_NAME FROM information_schema.statistics WHERE table_name =@Table", new { Table = table });
            if (!indexList.Contains(indexName))
            {
                var sql = $"CREATE INDEX {indexName} ON {table} ({string.Join(',', columns)});";
                await conn.ExecuteAsync(sql);
            }
        }

        public async Task CreateUniqueIndexIfNotExists(DataConnection conn, string table, string indexName, params string[] columns)
        {
            indexName = indexName.Replace('.', '_');
            var indexList = await conn.QueryToListAsync<string>("SELECT INDEX_NAME FROM information_schema.statistics WHERE table_name =@Table", new { Table = table });
            if (!indexList.Contains(indexName))
            {
                var sql = $"CREATE UNIQUE INDEX {indexName} ON {table} ({string.Join(',', columns)});";
                await conn.ExecuteAsync(sql);
            }
        }
    }
}
