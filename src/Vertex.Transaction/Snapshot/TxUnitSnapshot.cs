using System.Collections.Concurrent;
using Vertex.Abstractions.Serialization;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Abstractions.Snapshot;

namespace Vertex.Transaction.Snapshot
{
    public class TxUnitSnapshot<TInput> : ITxSnapshot<TxUnitSnapshot<TInput>>
    {
        public ConcurrentDictionary<string, DTxCommit<TInput>> RequestDict { get; set; } = new ConcurrentDictionary<string, DTxCommit<TInput>>();
        public TxUnitSnapshot<TInput> Clone(ISerializer serializer)
        {
            return new TxUnitSnapshot<TInput>
            {
                RequestDict = serializer.Deserialize<ConcurrentDictionary<string, DTxCommit<TInput>>>(serializer.Serialize(RequestDict))
            };
        }
    }
}
