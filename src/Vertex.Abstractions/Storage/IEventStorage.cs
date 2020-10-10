using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Storage
{
    public interface IEventStorage<PrimaryKey>
    {
        /// <summary>
        /// Single event insertion
        /// </summary>
        /// <param name="document"></param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task<bool> Append(EventDocument<PrimaryKey> document);

        /// <summary>
        /// Batch event insertion
        /// </summary>
        /// <param name="documents"></param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task TxAppend(List<EventDocument<PrimaryKey>> documents);
        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="startVersion">start version</param>
        /// <param name="endVersion">End version</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task<List<EventDocument<PrimaryKey>>> GetList(PrimaryKey actorId, long startVersion, long endVersion);
        /// <summary>
        /// Get events in batch
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="endTimestamp">< timestamp</param>
        /// <param name="skip">skip count</param>
        /// <param name="limit">take count</param>
        /// <returns></returns>
        Task<List<EventDocument<PrimaryKey>>> GetList(PrimaryKey actorId, long endTimestamp, int skip, int limit);
        /// <summary>
        /// Delete events before the specified version number(not contain endversion)
        /// </summary>
        /// <param name="stateId">State Id, equivalent to GrainId</param>
        /// <param name="endVersion">End version number</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task DeletePrevious(PrimaryKey stateId, long endVersion);

        /// <summary>
        /// Delete events after the specified version number
        /// </summary>
        /// <param name="actorId">State Id, equivalent to GrainId</param>
        /// <param name="startVersion">End version number</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        Task DeleteAfter(PrimaryKey actorId, long startVersion);
        /// <summary>
        /// 删除全部事件
        /// </summary>
        Task DeleteAll(PrimaryKey stateId);
    }
}