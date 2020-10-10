using System;

namespace Vertex.Transaction.Exceptions
{
    public class TxSnapshotException : Exception
    {
        public TxSnapshotException(string actorId, long snapshotVersion, long backupSnapshotVersion)
            : base($"StateId {actorId} and snapshot version {snapshotVersion} and backup snapshot version {backupSnapshotVersion}")
        {
        }
    }
}
