using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Serialization;
using Vertex.Utils;
using Vertex.Utils.Channels;
using Vertex.Utils.PooledTask;

namespace Vertex.Runtime
{
    public static class Extensions
    {
        public static void AddVertex(this IServiceCollection serviceCollection)
        {
            serviceCollection.AutoAddSnapshotHandler();
            serviceCollection.AddTransient(typeof(IMpscChannel<>), typeof(ThreadChannel<>));
            serviceCollection.AddSingleton<ISerializer, DefaultJsonSerializer>();
            serviceCollection.AddSingleton<IEventTypeContainer, EventTypeContainer>();
            serviceCollection.AddSingleton(typeof(TaskSourcePool<>));
        }

        private static void AutoAddSnapshotHandler(this IServiceCollection serviceCollection)
        {
            foreach (var assembly in AssemblyHelper.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes().Where(t => typeof(ISnapshotHandlerBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !t.IsGenericType))
                {
                    var genericInterface = type.GetInterfaces().Where(t => typeof(ISnapshotHandlerBase).IsAssignableFrom(t) && t != typeof(ISnapshotHandlerBase)).FirstOrDefault();
                    if (genericInterface != null)
                    {
                        serviceCollection.AddSingleton(genericInterface, type);
                    }
                }
            }
        }
    }
}
