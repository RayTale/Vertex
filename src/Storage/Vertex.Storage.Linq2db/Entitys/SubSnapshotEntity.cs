using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entitys
{
    [Table]
    public class SubSnapshotEntity<PrimaryKey>
    {
        [PrimaryKey]
        [Column]
        [Column(DataType = DataType.VarChar, Length = 200)]
        public PrimaryKey Id { get; set; }
        [Column]
        public long DoingVersion { get; set; }
        [Column]
        public long Version { get; set; }
    }
}
