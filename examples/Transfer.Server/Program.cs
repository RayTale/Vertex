using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using AutoMapper;
using LinqToDB.Common;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Transfer.Grains;
using Transfer.Grains.Common;
using Transfer.Repository;
using Vertex.Runtime;
using Vertex.Runtime.InnerService;
using Vertex.Runtime.Options;
using Vertex.Storage.Linq2db;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.InMemory;
using Vertex.Stream.InMemory.Grains;
using Vertex.Stream.Kafka;
using Vertex.Stream.RabbitMQ;
using Vertex.Stream.RabbitMQ.Options;
using Vertex.Transaction;

namespace Transfer.Server
{
    public class Program
    {
        public static IConfiguration Configuration;

        public static Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            var host = CreateHost();
            return host.RunAsync();
        }

        private static IHost CreateHost()
        {
            var connectionString = Configuration.GetValue<string>("ConnectionStrings:Default");
            var advertisedIp = Configuration.GetValue<string>("AdvertisedIP");
            IPAddress advertisedIpAddress;
            if (advertisedIp.IsNullOrEmpty())
            {
                advertisedIpAddress = GetIp();
            }
            else
            {
                advertisedIpAddress = IPAddress.Parse(advertisedIp);
            }
            var siloPort = Configuration.GetValue<int?>("EndpointOptions:SiloPort");
            var gatewayPort = Configuration.GetValue<int?>("EndpointOptions:GatewayPort");
            TransferDbContext.ConnectionString = connectionString ?? "Server=localhost;Port=5432;Database=Vertex;User Id=postgres;Password=postgres;Pooling=true;MaxPoolSize=20;";
            Console.WriteLine("connectionString=>" + connectionString);
            Console.WriteLine("advertisedIPAddress=>" + advertisedIpAddress);
            Console.WriteLine("siloPort=>" + siloPort);
            Console.WriteLine("gatewayPort=>" + gatewayPort);
            return new HostBuilder()
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        .UseAdoNetClustering(delegate (AdoNetClusteringSiloOptions options)
                        {
                            options.ConnectionString = connectionString;
                            options.Invariant = "Npgsql";
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Transfer";
                        })
                        .Configure<EndpointOptions>(options =>
                        {
                            options.AdvertisedIPAddress = advertisedIpAddress;
                            options.SiloPort = siloPort ?? 11111;
                            options.GatewayPort = gatewayPort ?? 30000;
                            options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, options.GatewayPort);
                            options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, options.SiloPort);
                        })
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
                    serviceCollection.AddLinq2DbStorage(
                        config =>
                        {
                            // var memorySQLiteConnection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
                            // memorySQLiteConnection.Open();
                            // serviceCollection.AddSingleton(memorySQLiteConnection);
                            config.Connections = new Vertex.Storage.Linq2db.Options.ConnectionOptions[]
                            {
                         new Vertex.Storage.Linq2db.Options.ConnectionOptions
                         {
                            Name = Consts.CoreDbName,
                            ProviderName = "PostgreSQL",
                            ConnectionString = connectionString??"Server=localhost;Port=5432;Database=Vertex;User Id=postgres;Password=postgres;Pooling=true;MaxPoolSize=20;",
                         },

                                // new Vertex.Storage.Linq2db.Options.ConnectionOptions
                                // {
                                //    Name = Consts.CoreDbName,
                                //    ProviderName = "MySql",
                                //    ConnectionString = "Server=localhost;Database=Vertex;UserId=root;Password=root;pooling=true;maxpoolsize=50;ConnectionLifeTime=30;"
                                // },
                                //new Vertex.Storage.Linq2db.Options.ConnectionOptions
                                //{
                                //   Name = Consts.CoreDbName,
                                //   ProviderName = "SQLite.MS",
                                //   ConnectionString = "Data Source=Vertex.SQLite.db;"
                                //}
                            };
                        }, new EventArchivePolicy("month", (name, time) => $"Vertex_Archive_{name}_{DateTimeOffset.FromUnixTimeSeconds(time).ToString("yyyyMM")}".ToLower(), table => table.StartsWith("Vertex_Archive".ToLower())));

                    serviceCollection.AddRabbitMQStream(_ => { });
                    serviceCollection.Configure<RabbitOptions>(Configuration.GetSection("RabbitConfig"));
                    // serviceCollection.AddKafkaStream(
                    // config => { },
                    // config =>
                    // {
                    //    config.BootstrapServers = "localhost:9092";
                    // }, config =>
                    // {
                    //    config.BootstrapServers = "localhost:9092";
                    // });
                    //serviceCollection.AddInMemoryStream();
                    serviceCollection.Configure<GrainCollectionOptions>(options =>
                    {
                        options.CollectionAge = TimeSpan.FromMinutes(5);
                    });
                    serviceCollection.ConfigureAll<SubActorOptions>(options =>
                    {
                        options.SnapshotVersionInterval = 1;
                    });
                    //serviceCollection.Configure<SubActorOptions>(typeof(AccountFlow).FullName, options =>
                    //{
                    //    options.SnapshotVersionInterval = 10;
                    //});
                    serviceCollection.AddAutoMapper(typeof(Account));
                    serviceCollection.AddSingleton(Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddConsole();
                }).Build();
        }

        private static IPAddress GetIp()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .FirstOrDefault(p =>
                    p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address))?.Address;
        }
    }
}
