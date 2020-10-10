using System;

namespace Vertex.Runtime.Exceptions
{
    public class SnapshotException : Exception
    {
        public SnapshotException(string id, Type grainType, long doingVersion, long snapshotVersion)
              : base($"{grainType.FullName} ({id})=>snapshotVersion= {snapshotVersion} ,doingVersion= {doingVersion}")
        {
        }
        public SnapshotException(string msg)
            : base(msg)
        {

        }
    }
}
