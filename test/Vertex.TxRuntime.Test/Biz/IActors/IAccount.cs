using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Options;
using Vertex.Transaction.Abstractions;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface IAccount : IVertexActor, IDTxActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();

        Task<ActorOptions> GetOptions();

        /// <summary>
        /// Get account balance
        /// </summary>
        /// <returns></returns>
        [AlwaysInterleave]
        Task<decimal> GetBalance();

        /// <summary>
        /// Increase account amount
        /// </summary>
        /// <param name="amount">amount</param>
        /// <param name="topupId">amount</param>
        /// <returns></returns>
        Task<bool> TopUp(decimal amount, string topupId);

        Task<bool> TopUp(decimal amount);

        /// <summary>
        /// Final consistent transfer
        /// </summary>
        /// <param name="toAccountId">target account ID</param>
        /// <param name="amount">transfer amount</param>
        /// <returns></returns>
        Task<bool> Transfer(long toAccountId, decimal amount);

        /// <summary>
        /// Transfer to account
        /// </summary>
        /// <param name="amount">Amount to account</param>
        /// <returns></returns>
        Task TransferArrived(decimal amount);

        /// <summary>
        /// Refund for failed transfer
        /// </summary>
        /// <param name="amount">amount</param>
        /// <returns></returns>
        Task<bool> TransferRefunds(decimal amount);

        Task HandlerError_Test();
    }
}