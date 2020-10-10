using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Storage
{
    public interface IEventArchive<PrimaryKey>
    {
        Task Arichive(IList<EventDocument<PrimaryKey>> documents);
        Task<List<EventDocument<PrimaryKey>>> GetList(PrimaryKey actorId, long startVersion, long endVersion);
    }
}
