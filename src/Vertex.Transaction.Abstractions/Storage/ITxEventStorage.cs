using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Abstractions.Storage
{
    public interface ITxEventStorage<PrimaryKey>
    {
        /// <summary>
        /// Single event insertion
        /// </summary>
        /// <param name="storageUnit"></param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task<bool> Append(EventDocument<PrimaryKey> storageUnit);
        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="stateId">State Id, equivalent to GrainId</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task<IList<EventDocument<PrimaryKey>>> GetLatest(PrimaryKey actorId, int limit);
        /// <summary>
        /// Delete events before the specified version number
        /// </summary>
        /// <param name="stateId">State Id, equivalent to GrainId</param>
        /// <param name="toVersion">End version number</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task DeletePrevious(PrimaryKey stateId, long toVersion);
    }
}
