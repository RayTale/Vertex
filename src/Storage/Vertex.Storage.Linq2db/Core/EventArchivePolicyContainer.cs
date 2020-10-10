using System.Collections.Generic;

namespace Vertex.Storage.Linq2db.Core
{
    public class EventArchivePolicyContainer
    {
        public Dictionary<string, EventArchivePolicy> Container { get; set; } = new Dictionary<string, EventArchivePolicy>();
    }
}
