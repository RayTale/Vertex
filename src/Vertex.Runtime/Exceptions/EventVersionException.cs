using System;

namespace Vertex.Runtime.Exceptions
{
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

        public string GrainTypeName { get; set; }

        public string ActorId { get; set; }

        public long SnapshotVersion { get; set; }

        public long EventVersion { get; set; }
    }
}
