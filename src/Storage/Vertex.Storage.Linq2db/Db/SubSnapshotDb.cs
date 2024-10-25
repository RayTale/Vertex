using LinqToDB;
using LinqToDB.Data;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Db
{
    public class SubSnapshotDb : DataConnection
    {
        public SubSnapshotDb(string name)
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

        public ITable<SubSnapshotEntity<TPrimaryKey>> Table<TPrimaryKey>() => this.GetTable<SubSnapshotEntity<TPrimaryKey>>();
    }
}
