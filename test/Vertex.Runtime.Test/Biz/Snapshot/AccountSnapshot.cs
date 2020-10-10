using Vertex.Abstractions.Snapshot;

namespace Vertex.Runtime.Test.Snapshot
{
    public class AccountSnapshot : ISnapshot
    {
        public decimal Balance { get; set; }
    }
}
