using System;
using Orleans;

namespace Vertex.Runtime.Exceptions
{
    [GenerateSerializer]
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

        [Id(0)]
        public string GrainTypeName { get; set; }

        [Id(1)]
        public string ActorId { get; set; }

        [Id(2)]
        public long SnapshotVersion { get; set; }

        [Id(3)]
        public long DoingVersion { get; set; }
    }
}
