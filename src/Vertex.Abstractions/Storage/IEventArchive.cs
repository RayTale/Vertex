using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Storage
{
    public interface IEventArchive<TPrimaryKey>
    {
        Task Arichive(IList<EventDocument<TPrimaryKey>> documents);

        Task<List<EventDocument<TPrimaryKey>>> GetList(TPrimaryKey actorId, long startVersion, long endVersion);
    }
}
