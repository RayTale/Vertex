using System;

namespace Vertex.Transaction.Exceptions
{
    public class TxSnapshotException : Exception
    {
        public TxSnapshotException(string actorId, long snapshotVersion, long backupSnapshotVersion)
        {
            this.ActorId = actorId;
            this.SnapshotVersion = snapshotVersion;
            this.BackupSnapshotVersion = backupSnapshotVersion;
        }

        public string ActorId { get; set; }

        public long SnapshotVersion { get; set; }

        public long BackupSnapshotVersion { get; set; }
    }
}
