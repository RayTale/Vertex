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
            return conn.DataProvider.ConnectionNamespace switch
            {
                ConnectionNamespace.PostgreSQL => new PGIndexGenerator(),
                ConnectionNamespace.MySql => new MySqlIndexGenerator(),
                ConnectionNamespace.SQLite => new SQLiteIndexGenerator(),
                _ => throw new ArgumentOutOfRangeException(conn.DataProvider.ConnectionNamespace),
            };
        }
    }
}
