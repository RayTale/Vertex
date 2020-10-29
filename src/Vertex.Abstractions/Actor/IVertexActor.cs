using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Actor
{
    public interface IVertexActor
    {
        [AlwaysInterleave]
        Task<IList<EventDocumentDto>> GetEventDocuments(long startVersion, long endVersion);
    }
}
