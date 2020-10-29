using System;
using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Storage;
using Vertex.Storage.Linq2db.Core;
using Vertex.Storage.Linq2db.Db;
using Vertex.Storage.Linq2db.Options;
using Vertex.Storage.Linq2db.Storage;
using Vertex.Transaction.Abstractions.Storage;

namespace Vertex.Storage.Linq2db
{
    public static class Extensions
    {
        public static void AddLinq2DbStorage(this IServiceCollection serviceCollection, Action<DbPoolOptions> configAction, params EventArchivePolicy[] archivePolicies)
        {
            serviceCollection.Configure<DbPoolOptions>(config => configAction(config));
            serviceCollection.AddSingleton<IEventArchiveFactory, EventArchiveFactory>();
            serviceCollection.AddSingleton<IEventStorageFactory, EventStorageFactory>();
            serviceCollection.AddSingleton<ISnapshotStorageFactory, SnapshotStorageFactory>();
            serviceCollection.AddSingleton<ISubSnapshotStorageFactory, SubSnapshotStorageFactory>();
            serviceCollection.AddSingleton<ITxEventStorageFactory, TxEventStorageFactory>();
            serviceCollection.AddSingleton<DbFactory>();
            var eventArchiveContainer = new EventArchivePolicyContainer();
            foreach (var policy in archivePolicies)
            {
                eventArchiveContainer.Container.Add(policy.Name, policy);
            }
            serviceCollection.AddSingleton(eventArchiveContainer);
        }
    }
}
