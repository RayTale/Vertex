using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Snapshot;

namespace Vertex.Transaction
{
    public static class Extensions
    {
        public static void AddTxUnitHandler<PrimaryKey, TRequest>(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(ISnapshotHandler<PrimaryKey, TxUnitSnapshot<TRequest>>), typeof(TxUnitSnapshotHandler<PrimaryKey, TRequest>));
        }
    }
}
