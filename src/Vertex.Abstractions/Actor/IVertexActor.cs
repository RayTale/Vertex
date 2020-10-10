using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Abstractions.Actor
{
    public interface IVertexActor
    {
        [AlwaysInterleave]
        Task<IList<EventDocumentDto>> GetEventDocuments(long startVersion, long endVersion);
    }
}
