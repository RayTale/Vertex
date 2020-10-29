using System;
using IdGen;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Vertex.Runtime;

namespace Vertex.TxRuntime.Core
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
            this.Provider = serviceCollection.BuildServiceProvider();
        }

        public TestCluster Cluster { get; private set; }

        public ServiceProvider Provider { get; private set; }

        public IdGenerator ActorIdGen { get; } = new IdGenerator(0, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }
    }
}
