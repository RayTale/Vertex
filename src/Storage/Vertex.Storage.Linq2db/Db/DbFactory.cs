using LinqToDB.Data;
using Microsoft.Extensions.Options;
using Vertex.Storage.Linq2db.Options;

namespace Vertex.Storage.Linq2db.Db
{
    public class DbFactory
    {
        public DbFactory(IOptions<DbPoolOptions> dbOptions)
        {
            DataConnection.DefaultSettings = new DbSettings(dbOptions.Value);
        }

        public EventDb GetEventDb(string name)
        {
            return new EventDb(name);
        }

        public SnapshotDb GetSnapshotDb(string name)
        {
            return new SnapshotDb(name);
        }

        public SubSnapshotDb GetSubSnapshotDb(string name)
        {
            return new SubSnapshotDb(name);
        }
    }
}
