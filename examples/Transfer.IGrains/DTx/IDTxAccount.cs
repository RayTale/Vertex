using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.Actor;
using Vertex.Transaction.Abstractions;

namespace Transfer.IGrains.DTx
{
    public interface IDTxAccount : IVertexActor, IDTxActor, IGrainWithIntegerKey
    {
        /// <summary>
        /// Get account balance
        /// </summary>
        /// <returns></returns>
        [AlwaysInterleave]
        Task<decimal> GetBalance();

        Task TopUp(decimal amount, string flowId);

        Task<bool> Transfer(long toAccountId, decimal amount);

        Task TransferArrived(decimal amount);
    }
}
