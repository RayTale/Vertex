using System;

namespace Vertex.Protocol
{
    public readonly ref struct EventTransUnit
    {
        public EventTransUnit(
            string eventName,
            object actorId,
            Span<byte> baseBytes,
            Span<byte> eventBytes)
        {
            this.EventName = eventName;
            this.ActorId = actorId;
            this.MetaBytes = baseBytes;
            this.EventBytes = eventBytes;
        }

        /// <summary>
        /// 事件唯一名称
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// 事件GrainId
        /// </summary>
        public object ActorId { get; }

        /// <summary>
        /// 事件Meta信息的bytes
        /// </summary>
        public Span<byte> MetaBytes { get; }

        /// <summary>
        /// 事件本身的bytes
        /// </summary>
        public Span<byte> EventBytes { get; }
    }
}
