using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Vertex.Storage.Linq2db.Options
{
    public class DbSettings : ILinqToDBSettings
    {
        readonly DbPoolOptions dbPoolOptions;
        public DbSettings(DbPoolOptions dbPoolOptions)
        {
            this.dbPoolOptions = dbPoolOptions;
        }
        public string DefaultConfiguration => this.dbPoolOptions.Connections.First().ProviderName;
        public string DefaultDataProvider => this.dbPoolOptions.Connections.First().ProviderName;

        public IEnumerable<IConnectionStringSettings> ConnectionStrings => this.dbPoolOptions.Connections;

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();
    }
}
