using LinqToDB.Data;
using System;
using Vertex.Storage.Linq2db.Index.MySql;
using Vertex.Storage.Linq2db.Index.Postgresql;
using Vertex.Storage.Linq2db.Index.SQLite;
using Vertex.Storage.Linq2db.Index.SqlServer;

namespace Vertex.Storage.Linq2db.Index
{
    public static class IndexGenFactory
    {
        public static IIndexGenerator GetGenerator(this DataConnection conn)
        {
            if (conn.DataProvider.Name.StartsWith(DbProviderName.PostgreSQL))
                return new PGIndexGenerator();
            if (conn.DataProvider.Name.StartsWith(DbProviderName.MySql))
                return new MySqlIndexGenerator();
            if (conn.DataProvider.Name.StartsWith(DbProviderName.SQLite)
                || conn.DataProvider.Name.StartsWith(DbProviderName.MSSQLite))
                return new SQLiteIndexGenerator();
            if (conn.DataProvider.Name.StartsWith(DbProviderName.SqlServer))
                return new SqlServerIndexGenerator();
            throw new NotSupportedException(conn.DataProvider.Name);
        }
    }
}
