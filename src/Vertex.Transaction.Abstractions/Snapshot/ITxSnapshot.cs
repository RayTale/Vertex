using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Transaction.Abstractions.Snapshot
{
    public interface ITxSnapshot<T> : ISnapshot
    {
        T Clone(ISerializer serializer);
    }
}
