using System;

namespace Vertex.Runtime.Exceptions
{
    public class SnapshotException : Exception
    {
        public SnapshotException(string id, Type grainType, long doingVersion, long snapshotVersion)
            : base($"{grainType.Name} ({id})=>snapshotVersion= {snapshotVersion} ,doingVersion= {doingVersion}")
        {
            this.GrainTypeName = grainType.Name;
            this.ActorId = id;
            this.SnapshotVersion = snapshotVersion;
            this.DoingVersion = doingVersion;
        }

        public string GrainTypeName { get; set; }

        public string ActorId { get; set; }

        public long SnapshotVersion { get; set; }

        public long DoingVersion { get; set; }
    }
}
