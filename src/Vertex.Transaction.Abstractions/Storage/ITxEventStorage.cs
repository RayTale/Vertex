using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Abstractions.Storage
{
    public interface ITxEventStorage<TPrimaryKey>
    {
        /// <summary>
        /// Single event insertion
        /// </summary>
        /// <param name="storageUnit">storage unit element</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> Append(EventDocument<TPrimaryKey> storageUnit);

        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="actorId">actor id</param>
        /// <param name="limit">page limit</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<IList<EventDocument<TPrimaryKey>>> GetLatest(TPrimaryKey actorId, int limit);

        /// <summary>
        /// Delete events before the specified version number
        /// </summary>
        /// <param name="stateId">State Id, equivalent to GrainId</param>
        /// <param name="toVersion">End version number</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeletePrevious(TPrimaryKey stateId, long toVersion);
    }
}
