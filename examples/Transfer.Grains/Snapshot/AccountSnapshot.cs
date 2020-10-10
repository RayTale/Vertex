using Vertex.Abstractions.Snapshot;

namespace Transfer.Grains.Snapshot
{
    public class AccountSnapshot : ISnapshot
    {
        public decimal Balance { get; set; }
    }
}
