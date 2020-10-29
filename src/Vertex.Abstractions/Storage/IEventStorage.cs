using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Storage
{
    public interface IEventStorage<TPrimaryKey>
    {
        /// <summary>
        /// Single event insertion
        /// </summary>
        /// <param name="document">doc</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> Append(EventDocument<TPrimaryKey> document);

        /// <summary>
        /// Batch event insertion
        /// </summary>
        /// <param name="documents">doc list</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task TxAppend(List<EventDocument<TPrimaryKey>> documents);

        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="startVersion">start version</param>
        /// <param name="endVersion">End version</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<EventDocument<TPrimaryKey>>> GetList(TPrimaryKey actorId, long startVersion, long endVersion);

        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="endTimestamp">end timestamp</param>
        /// <param name="skip">skip count</param>
        /// <param name="limit">take count</param>
        /// <returns></returns>
        Task<List<EventDocument<TPrimaryKey>>> GetList(TPrimaryKey actorId, long endTimestamp, int skip, int limit);

        /// <summary>
        /// Delete events before the specified version number(not contain endversion)
        /// </summary>
        /// <param name="stateId">State Id, equivalent to GrainId</param>
        /// <param name="endVersion">End version number</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeletePrevious(TPrimaryKey stateId, long endVersion);

        /// <summary>
        /// Delete events after the specified version number
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="startVersion">End version number</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAfter(TPrimaryKey actorId, long startVersion);

        /// <summary>
        /// 删除全部事件
        /// </summary>
        /// <param name="actorId">actor id</param>
        /// <returns></returns>
        Task DeleteAll(TPrimaryKey actorId);
    }
}