using Orleans;

namespace Vertex.Abstractions.Snapshot
{
    /// <summary>
    /// 快照元数据类
    /// </summary>
    [GenerateSerializer]
    public record SnapshotMeta<TPrimaryKey>
    {
        /// <summary>
        /// 演员ID
        /// </summary>
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

        /// <summary>
        /// 最小事件时间戳
        /// </summary>
        [Id(3)]
        public long MinEventTimestamp { get; set; }

        /// <summary>
        /// 最小事件版本
        /// </summary>
        [Id(4)]
        public long MinEventVersion { get; set; }

        /// <summary>
        /// 是否是最新的
        /// </summary>
        [Id(5)]
        public bool IsLatest { get; set; }
    }
}