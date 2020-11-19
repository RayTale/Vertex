using System;

namespace Vertex.Storage.Linq2db.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public abstract class SnapshotStorageBaseAttribute : Attribute
    {
        public abstract string GetTableName(string actorId);

        public abstract string GetOptionName(string actorId);
    }
}
