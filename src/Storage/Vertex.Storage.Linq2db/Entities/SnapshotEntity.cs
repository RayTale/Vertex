using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entities
{
    [Table]
    public class SnapshotEntity<PrimaryKey>
    {
        [PrimaryKey]
        [Column]
        [Column(DataType = DataType.VarChar, Length = 200)]
        public PrimaryKey Id { get; set; }
        [Column(DataType = DataType.Json)]
        public string Data { get; set; }
        [Column]
        public long DoingVersion { get; set; }
        [Column]
        public long Version { get; set; }
        [Column]
        public long MinEventTimestamp { get; set; }
        [Column]
        public long MinEventVersion { get; set; }
        [Column]
        public bool IsLatest { get; set; }
    }
}
