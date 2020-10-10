using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entitys
{
    [Table]
    public class EventEntity<PrimaryKey>
    {
        [Column]
        [Column(DataType = DataType.VarChar, Length = 200)]
        public PrimaryKey ActorId { get; set; }
        [Column(DataType = DataType.VarChar, Length = 200)]
        public string Name { get; set; }
        [Column(DataType = DataType.Json)]
        public string Data { get; set; }
        [Column(DataType = DataType.VarChar, Length = 200)]
        public string FlowId { get; set; }
        [Column]
        public long Version { get; set; }
        [Column]
        public long Timestamp { get; set; }
    }
}
