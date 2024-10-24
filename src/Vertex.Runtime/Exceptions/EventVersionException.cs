using System;
using Orleans;

namespace Vertex.Runtime.Exceptions
{
    [GenerateSerializer]
    public class EventVersionException : Exception
    {
        public EventVersionException(string id, Type type, long eventVersion, long snapshotVersion)
         : base($"{type.Name}({id})=>SnapshotVersion={snapshotVersion},EventVerstion={eventVersion}")
        {
            this.GrainTypeName = type.Name;
            this.ActorId = id;
            this.SnapshotVersion = snapshotVersion;
            this.EventVersion = eventVersion;
        }

        [Id(0)]
        public string GrainTypeName { get; set; }

        [Id(1)]
        public string ActorId { get; set; }

        [Id(2)]
        public long SnapshotVersion { get; set; }

        [Id(3)]
        public long EventVersion { get; set; }
    }
}
