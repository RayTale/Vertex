using System;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using Vertex.Storage.Linq2db;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.InMemory;

namespace Vertex.Runtime.Core
{
    public class TestSiloConfigurations : ISiloConfigurator
    {
        public const string TestConnectionName = "vertex";

        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
            {
                services.AddVertex();
                services.AddInMemoryStream();
                services.AddLinq2DbStorage(config =>
                {
                    var memorySQLiteConnection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
                    memorySQLiteConnection.Open();
                    services.AddSingleton(memorySQLiteConnection);
                    config.Connections = new Storage.Linq2db.Options.ConnectionOptions[]
                    {
                        new Storage.Linq2db.Options.ConnectionOptions
                        {
                            Name = TestConnectionName,
                            ProviderName = "SQLite.MS",
                            ConnectionString = "Data Source=InMemorySample;Mode=Memory;Cache=Shared"
                        }
                    };
                }, new EventArchivePolicy("month", (name, time) => $"Vertex_Archive_{name}_{DateTimeOffset.FromUnixTimeSeconds(time):yyyyMM}".ToLower(), table => table.StartsWith("Vertex_Archive".ToLower())));
            }).AddSimpleMessageStreamProvider("SMSProvider", options => options.FireAndForgetDelivery = true).AddMemoryGrainStorage("PubSubStore");
        }
    }
}
