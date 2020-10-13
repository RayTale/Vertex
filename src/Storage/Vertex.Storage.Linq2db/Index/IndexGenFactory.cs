using LinqToDB.Data;
using System;
using Vertex.Storage.Linq2db.Index.MySql;
using Vertex.Storage.Linq2db.Index.Postgresql;
using Vertex.Storage.Linq2db.Index.SQLite;

namespace Vertex.Storage.Linq2db.Index
{
    public static class IndexGenFactory
    {
        public static IIndexGenerator GetGenerator(this DataConnection conn)
        {
            return conn.DataProvider.Name switch
            {
                DbProviderName.PostgreSQL => new PGIndexGenerator(),
                DbProviderName.MySql => new MySqlIndexGenerator(),
                DbProviderName.SQLite or DbProviderName.MSSQLite => new SQLiteIndexGenerator(),
                _ => throw new NotSupportedException(conn.DataProvider.Name),
            };
        }
    }
}
