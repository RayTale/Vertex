using System;
using System.Text;
using Vertex.Abstractions.Event;
using Vertex.Utils;

namespace Vertex.Runtime.Event
{
    public static class EventExtensions
    {
        public static SharedArray ConvertToBytes(this EventMeta metaInfo)
        {
            var buffer = SharedArray.Rent();
            buffer.Write(metaInfo.Version);
            buffer.Write(metaInfo.Timestamp);
            buffer.WriteUtf8String(metaInfo.FlowId);
            return buffer;
        }

        public static EventMeta ParseToEventMeta(this Span<byte> bytes)
        {
            return new EventMeta
            {
                Version = BitConverter.ToInt64(bytes),
                Timestamp = BitConverter.ToInt64(bytes[sizeof(long)..]),
                FlowId = Encoding.UTF8.GetString(bytes.Slice(sizeof(long) * 2))
            };
        }
    }
}
