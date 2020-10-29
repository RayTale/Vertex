using System;
using Vertex.Abstractions.Serialization;
using Vertex.Runtime.Snapshot;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Events.TxUnit;

namespace Vertex.Transaction.Snapshot
{
    public class TxUnitSnapshotHandler<TPrimaryKey, TInput> : SnapshotHandlerBase<TPrimaryKey, TxUnitSnapshot<TInput>>
    {
        private readonly ISerializer serializer;

        public TxUnitSnapshotHandler(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public void EventHandle(TxUnitSnapshot<TInput> state, UnitCommitEvent evt)
        {
            var data = this.serializer.Deserialize<TInput>(evt.Data);
            if (!state.RequestDict.TryAdd(evt.TxId, new DTxCommit<TInput>
            {
                TxId = evt.TxId,
                Data = data,
                Status = TransactionStatus.WaitingCommit,
                Timestamp = evt.StartTime
            }))
            {
                throw new ArgumentOutOfRangeException(evt.TxId);
            }
        }

        public void EventHandle(TxUnitSnapshot<TInput> state, UnitCommitedEvent evt)
        {
            if (!state.RequestDict.TryGetValue(evt.TxId, out var commit))
            {
                throw new ArgumentOutOfRangeException(evt.TxId);
            }
            commit.Status = TransactionStatus.Commited;
        }

        public void EventHandle(TxUnitSnapshot<TInput> state, UnitFinishedEvent evt)
        {
            if (!state.RequestDict.TryRemove(evt.TxId, out var _))
            {
                throw new ArgumentOutOfRangeException(evt.TxId);
            }
        }
    }
}
