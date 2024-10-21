using Orleans;

namespace Vertex.Abstractions.Event
{
    /// <summary>
    /// 记录事件的元信息
    /// </summary>
    [GenerateSerializer]
    public class EventMeta
    {
        /// <summary>
        /// 流程ID
        /// </summary>
        [Id(0)]
        public string FlowId { get; set; }

        /// <summary>
        /// 用于描述由演员生成的事件序列
        /// </summary>
        [Id(1)]
        public long Version { get; init; }

        /// <summary>
        /// 记录事件生成的时间戳，精确到秒
        /// </summary>
        [Id(2)]
        public long Timestamp { get; init; }
    }
}