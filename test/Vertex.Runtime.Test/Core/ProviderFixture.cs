using System;
using Microsoft.Extensions.DependencyInjection;

namespace Vertex.Runtime.Core
{
    public class ProviderFixture : IDisposable
    {
        public ProviderFixture()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddVertex();
            serviceCollection.AddLogging();
            this.Provider = serviceCollection.BuildServiceProvider();
        }

        public ServiceProvider Provider { get; private set; }

        public void Dispose()
        {
            this.Provider.Dispose();
        }
    }
}
