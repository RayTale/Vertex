using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entities
{
    [Table]
    public class EventEntity<TPrimaryKey>
    {
        [Column]
        [Column(DataType = DataType.VarChar, Length = 200)]
        public TPrimaryKey ActorId { get; set; }

        [Column(DataType = DataType.VarChar, Length = 200)]
        public string Name { get; set; }

        [Column(DataType = DataType.Text)]
        public string Data { get; set; }

        [Column(DataType = DataType.VarChar, Length = 200)]
        public string FlowId { get; set; }

        [Column]
        public long Version { get; set; }

        [Column]
        public long Timestamp { get; set; }
    }
}
