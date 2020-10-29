using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Options;
using Vertex.Runtime.Test.Snapshot;

namespace Vertex.Runtime.Test.IActors
{
    public interface IAccount : IVertexActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();

        Task<long> GetActivateSnapshotVersion();

        Task<ActorOptions> GetOptions();

        Task<ArchiveOptions> GetArchiveOptions();

        Task SetArchiveOptions(ArchiveOptions archiveOptions);

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
        /// <param name="topupId">topupId</param>
        /// <returns></returns>
        Task<bool> TopUp(decimal amount, string topupId);

        Task<bool> TopUp(decimal amount);

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

        Task RecoverySnapshot_Test();

        Task Deactivate_Test();

        Task<(bool can, long endTimestamp)> CanArchive_Test();

        Task Archive_Test(long endTimestamp);

        Task<int> GetArchiveEventCount();

        Task<List<EventDocumentDto>> GetEventDocuments_FromEventStorage(long startVersion, long endVersion);

        Task HandlerError_Test();
    }
}