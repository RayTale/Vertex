using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.EventStream;
using Vertex.Stream.Common;
using Vertex.Stream.InMemory.Consumer;

namespace Vertex.Stream.InMemory
{
    public static class Extensions
    {
        public static void AddInMemoryStream(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IEventStreamFactory, EventStreamFactory>();
            serviceCollection.AddSingleton<IStreamSubHandler, StreamSubHandler>();
            serviceCollection.AddHostedService<ConsumerManager>();
        }
    }
}
