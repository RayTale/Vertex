using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entities
{
    [Table]
    public class SnapshotEntity<TPrimaryKey>
    {
        [PrimaryKey]
        [Column(Length = 200, CanBeNull = false)]
        public TPrimaryKey Id { get; set; }

        [Column(DataType = DataType.Text)]
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
