using System;

namespace Vertex.Runtime.Exceptions
{
    public class EventVersionException : Exception
    {
        public EventVersionException(string id, Type type, long eventVersion, long snapshotVersion)
         : base($"{type.FullName}({id})=>SnapshotVersion={snapshotVersion},EventVerstion={eventVersion}")
        {
        }
    }
}
