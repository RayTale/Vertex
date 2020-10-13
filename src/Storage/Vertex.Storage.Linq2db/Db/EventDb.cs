using LinqToDB;
using LinqToDB.Data;
using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Db
{
    public class EventDb : DataConnection
    {
        public EventDb(string name) : base(name)
        {
            this.MappingSchema.EntityDescriptorCreatedCallback = (schema, entityDescriptor) =>
            {
                entityDescriptor.TableName = entityDescriptor.TableName.ToLower();
                foreach (var entityDescriptorColumn in entityDescriptor.Columns)
                {
                    entityDescriptorColumn.ColumnName = entityDescriptorColumn.ColumnName.ToLower();
                }
            };
        }
        public ITable<EventEntity<PrimaryKey>> Table<PrimaryKey>() => GetTable<EventEntity<PrimaryKey>>();
    }
}
