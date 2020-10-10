using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Transfer.IGrains.Common
{
    public interface IAccount : IVertexActor, IGrainWithIntegerKey
    {
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
        /// <summary>
        /// Final consistent transfer
        /// </summary>
        /// <param name="toAccountId">target account ID</param>
        /// <param name="amount">transfer amount</param>
        /// <param name="transferId">transfer id</param>
        /// <returns></returns>
        Task<bool> Transfer(long toAccountId, decimal amount, string transferId);
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
    }
}