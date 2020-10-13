using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Transfer.Grains;
using Transfer.Grains.Common;
using Transfer.IGrains.DTx;
using Vertex.Runtime;
using Vertex.Runtime.InnerService;
using Vertex.Storage.Linq2db;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.InMemory;
using Vertex.Stream.InMemory.Grains;
using Vertex.Stream.Kafka;
using Vertex.Stream.RabbitMQ;
using Vertex.Transaction;

namespace Transfer.Server
{
    class Program
    {
        static Task Main(string[] args)
        {
            var host = CreateHost();
            return host.RunAsync();
        }
        private static IHost CreateHost()
        {
            return new HostBuilder()
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Transfer";
                        })
                        .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                        .ConfigureApplicationParts(parts =>
                        {
                            parts.AddApplicationPart(typeof(Account).Assembly).WithReferences();
                            parts.AddApplicationPart(typeof(DIDActor).Assembly).WithReferences();
                            parts.AddApplicationPart(typeof(StreamIdActor).Assembly).WithReferences();
                        })
                        .AddSimpleMessageStreamProvider("SMSProvider", options => options.FireAndForgetDelivery = true).AddMemoryGrainStorage("PubSubStore");
                })
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddVertex();
                    serviceCollection.AddTxUnitHandler<long, TransferRequest>();
                    serviceCollection.AddLinq2DbStorage(config =>
                    {
                        var memorySQLiteConnection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
                        memorySQLiteConnection.Open();
                        serviceCollection.AddSingleton(memorySQLiteConnection);
                        config.Connections = new Vertex.Storage.Linq2db.Options.ConnectionOptions[] {
                        //new Vertex.Storage.Linq2db.Options.ConnectionOptions
                        //{
                        //    Name = Consts.core_db_Name,
                        //    ProviderName = "PostgreSQL",
                        //    ConnectionString = "Server=localhost;Port=5432;Database=Vertex;User Id=postgres;Password=postgres;Pooling=true;MaxPoolSize=20;"
                        //},
                        //new Vertex.Storage.Linq2db.Options.ConnectionOptions
                        //{
                        //    Name = Consts.core_db_Name,
                        //    ProviderName = "MySql",
                        //    ConnectionString = "Server=localhost;Database=Vertex;UserId=root;Password=root;pooling=true;maxpoolsize=50;ConnectionLifeTime=30;"
                        //},
                        new Vertex.Storage.Linq2db.Options.ConnectionOptions
                        {
                            Name = Consts.core_db_Name,
                            ProviderName = "SQLite.MS",
                            ConnectionString = "Data Source=InMemorySample;Mode=Memory;Cache=Shared"
                        }
                        };
                    }, new EventArchivePolicy("month", (name, time) => $"Vertex_Archive_{name}_{DateTimeOffset.FromUnixTimeSeconds(time).ToString("yyyyMM")}".ToLower(), table => table.StartsWith("Vertex_Archive".ToLower())));
                    //serviceCollection.AddRabbitMQStream(options =>
                    //{
                    //    options.VirtualHost = "/";
                    //    options.Hosts = new string[] { "localhost:5672" };
                    //    options.UserName = "guest";
                    //    options.Password = "guest";
                    //});
                    //serviceCollection.AddKafkaStream(
                    //config => { },
                    //config =>
                    //{
                    //    config.BootstrapServers = "localhost:9092";
                    //}, config =>
                    //{
                    //    config.BootstrapServers = "localhost:9092";
                    //});
                    serviceCollection.AddInMemoryStream();
                    serviceCollection.Configure<GrainCollectionOptions>(options =>
                    {
                        options.CollectionAge = TimeSpan.FromMinutes(5);
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddConsole();
                }).Build();
        }
    }
}
