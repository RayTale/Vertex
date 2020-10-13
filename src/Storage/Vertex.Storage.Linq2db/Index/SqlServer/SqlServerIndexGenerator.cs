using LinqToDB.Data;
using System.Threading.Tasks;

namespace Vertex.Storage.Linq2db.Index.SqlServer
{
    public class SqlServerIndexGenerator : IIndexGenerator
    {
        public async Task CreateIndexIfNotExists(DataConnection conn, string table, string indexName, params string[] columns)
        {
            indexName = indexName.Replace('.', '_');
            var indexCount = await conn.ExecuteAsync<int>("SELECT count(*) FROM sys.indexes WHERE name = @IndexName AND object_id = OBJECT_ID(@Table)", new { Table = table, IndexName = indexName });
            if (indexCount == 0)
            {
                var sql = $"CREATE INDEX {indexName} ON {table} ({string.Join(',', columns)});";
                await conn.ExecuteAsync(sql);
            }
        }

        public async Task CreateUniqueIndexIfNotExists(DataConnection conn, string table, string indexName, params string[] columns)
        {
            indexName = indexName.Replace('.', '_');
            var indexCount = await conn.ExecuteAsync<int>("SELECT count(*) FROM sys.indexes WHERE name = @IndexName AND object_id = OBJECT_ID(@Table)", new { Table = table, IndexName = indexName });
            if (indexCount == 0)
            {
                var sql = $"CREATE UNIQUE INDEX {indexName} ON {table} ({string.Join(',', columns)});";
                await conn.ExecuteAsync(sql);
            }
        }
    }
}
