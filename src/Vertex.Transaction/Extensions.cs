using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Snapshot;

namespace Vertex.Transaction
{
    public static class Extensions
    {
        public static void AddTxUnitHandler<TPrimaryKey, TRequest>(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(ISnapshotHandler<TPrimaryKey, TxUnitSnapshot<TRequest>>), typeof(TxUnitSnapshotHandler<TPrimaryKey, TRequest>));
        }
    }
}
