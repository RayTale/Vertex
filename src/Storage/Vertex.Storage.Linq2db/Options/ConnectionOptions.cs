using LinqToDB.Configuration;

namespace Vertex.Storage.Linq2db.Options
{
    public class ConnectionOptions : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }

        public string Name { get; set; }

        public string ProviderName { get; set; }

        public bool IsGlobal => false;
    }
}
