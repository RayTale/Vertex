using LinqToDB;
using LinqToDB.Data;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Db
{
    public class SnapshotDb : DataConnection
    {
        public SnapshotDb(string name)
            : base(name)
        {
            LinqToDB.Mapping.MappingSchema.EntityDescriptorCreatedCallback = (schema, entityDescriptor) =>
            {
                entityDescriptor.TableName = entityDescriptor.TableName.ToLower();
                foreach (var entityDescriptorColumn in entityDescriptor.Columns)
                {
                    entityDescriptorColumn.ColumnName = entityDescriptorColumn.ColumnName.ToLower();
                }
            };
        }

        public ITable<SnapshotEntity<TPrimaryKey>> Table<TPrimaryKey>() => this.GetTable<SnapshotEntity<TPrimaryKey>>();
    }
}