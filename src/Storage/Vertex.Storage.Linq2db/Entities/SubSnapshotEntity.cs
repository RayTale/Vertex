using LinqToDB;
using LinqToDB.Mapping;

namespace Vertex.Storage.Linq2db.Entities
{
    [Table]
    public class SubSnapshotEntity<TPrimaryKey>
    {
        [PrimaryKey]
        [Column]
        [Column(DataType = DataType.VarChar, Length = 200)]
        public TPrimaryKey Id { get; set; }

        [Column]
        public long DoingVersion { get; set; }

        [Column]
        public long Version { get; set; }
    }
}
