using Orleans;

namespace Vertex.Abstractions.Snapshot
{
    /// <summary>
    /// 子快照类
    /// </summary>
    [GenerateSerializer]
    public record SubSnapshot<TPrimaryKey>
    {
        [Id(0)]
        public TPrimaryKey ActorId { get; set; }

        /// <summary>
        /// 正在处理的版本
        /// </summary>
        [Id(1)]
        public long DoingVersion { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [Id(2)]
        public long Version { get; set; }
    }
}