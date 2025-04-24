using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Vertex.Storage.Linq2db.Entities;

namespace Vertex.Storage.Linq2db.Db
{
    public class EventDb : DataConnection
    {
        public EventDb(string name)
            : base(name)
        {
            MappingSchema.EntityDescriptorCreatedCallback = (schema, entityDescriptor) =>
            {
                entityDescriptor.TableName = entityDescriptor.TableName.ToLower();
                foreach (var entityDescriptorColumn in entityDescriptor.Columns)
                {
                    entityDescriptorColumn.ColumnName = entityDescriptorColumn.ColumnName.ToLower();
                }
            };
        }

        public ITable<EventEntity<TPrimaryKey>> Table<TPrimaryKey>() => this.GetTable<EventEntity<TPrimaryKey>>();
    }
}
