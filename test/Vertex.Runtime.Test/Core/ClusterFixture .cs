using IdGen;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using Vertex.Storage.Linq2db;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.InMemory;

namespace Vertex.Runtime.Core
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
            this.Cluster = builder.Build();
            this.Cluster.Deploy();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddVertex();
            serviceCollection.AddLogging();
            Provider = serviceCollection.BuildServiceProvider();
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
        public ServiceProvider Provider { get; private set; }
        public IdGenerator ActorIdGen { get; } = new IdGenerator(0, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));
    }

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
                    config.Connections = new Storage.Linq2db.Options.ConnectionOptions[] {
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
