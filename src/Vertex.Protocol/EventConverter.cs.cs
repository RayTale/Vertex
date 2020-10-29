using System;
using System.Text;
using Vertex.Utils;

namespace Vertex.Protocol
{
    public static class EventConverter
    {
        private const int BytesHeadLength = 2;

        public static SharedArray ConvertToBytes(this in EventTransUnit eventBitUnit)
        {
            var buffer = SharedArray.Rent();
            var eventNameSpan = eventBitUnit.EventName.AsSpan();
            var (actorIdBytes, idType) = eventBitUnit.ActorId switch
            {
                long id => (actorIdBytes: BitConverter.GetBytes(id), idType: ActorIdType.Long),
                string id => (actorIdBytes: Encoding.UTF8.GetBytes(id), idType: ActorIdType.String),
                Guid id => (actorIdBytes: Encoding.UTF8.GetBytes(id.ToString()), idType: ActorIdType.Guid),
                _ => throw new ArgumentOutOfRangeException(eventBitUnit.ActorId.GetType().Name),
            };
            buffer.Write((byte)BytesHeaderType.Event);
            buffer.Write((byte)idType);
            buffer.Write((ushort)Encoding.UTF8.GetByteCount(eventNameSpan));
            buffer.Write((ushort)actorIdBytes.Length);
            buffer.Write((ushort)eventBitUnit.MetaBytes.Length);
            buffer.Write(eventBitUnit.EventBytes.Length);
            buffer.WriteUtf8String(eventNameSpan);
            buffer.Write(actorIdBytes);
            buffer.Write(eventBitUnit.MetaBytes);
            buffer.Write(eventBitUnit.EventBytes);
            return buffer;
        }

        public static bool TryParseActorId(byte[] bytes, out object primaryKey)
        {
            if (bytes[0] == (byte)BytesHeaderType.Event)
            {
                var idType = (ActorIdType)bytes[1];
                var bytesSpan = bytes.AsSpan();
                var eventTypeLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength, sizeof(ushort)));
                var actorIdBytesLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength + sizeof(ushort), sizeof(ushort)));
                var skipLength = BytesHeadLength + (3 * sizeof(ushort)) + sizeof(int);

                switch (idType)
                {
                    case ActorIdType.Long:
                        {
                            primaryKey = BitConverter.ToInt64(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength));
                            return true;
                        }

                    case ActorIdType.String:
                        {
                            primaryKey = Encoding.UTF8.GetString(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength));
                            return true;
                        }

                    case ActorIdType.Guid:
                        {
                            primaryKey = new Guid(Encoding.UTF8.GetString(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength)));
                            return true;
                        }

                    default: throw new NotSupportedException(bytes[1].ToString());
                }
            }

            primaryKey = default;
            return false;
        }

        public static bool TryParseWithNoId(byte[] bytes, out EventTransUnit value)
        {
            if (bytes[0] == (byte)BytesHeaderType.Event)
            {
                var bytesSpan = bytes.AsSpan();
                var eventTypeLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength, sizeof(ushort)));
                var actorIdBytesLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength + sizeof(ushort), sizeof(ushort)));
                var baseBytesLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength + (2 * sizeof(ushort)), sizeof(ushort)));
                var eventBytesLength = BitConverter.ToInt32(bytesSpan.Slice(BytesHeadLength + (3 * sizeof(ushort)), sizeof(int)));
                var skipLength = BytesHeadLength + (3 * sizeof(ushort)) + sizeof(int);

                value = new EventTransUnit(
                Encoding.UTF8.GetString(bytesSpan.Slice(skipLength, eventTypeLength)),
                null,
                bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength, baseBytesLength),
                bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength + baseBytesLength, eventBytesLength));
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryParse(byte[] bytes, out EventTransUnit value)
        {
            if (bytes[0] == (byte)BytesHeaderType.Event)
            {
                var idType = (ActorIdType)bytes[1];
                var bytesSpan = bytes.AsSpan();
                var eventTypeLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength, sizeof(ushort)));
                var actorIdBytesLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength + sizeof(ushort), sizeof(ushort)));
                var baseBytesLength = BitConverter.ToUInt16(bytesSpan.Slice(BytesHeadLength + (2 * sizeof(ushort)), sizeof(ushort)));
                var eventBytesLength = BitConverter.ToInt32(bytesSpan.Slice(BytesHeadLength + (3 * sizeof(ushort)), sizeof(int)));
                var skipLength = BytesHeadLength + (3 * sizeof(ushort)) + sizeof(int);
                if (idType == ActorIdType.Long)
                {
                    var actorId = BitConverter.ToInt64(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength));
                    value = new EventTransUnit(
                        Encoding.UTF8.GetString(bytesSpan.Slice(skipLength, eventTypeLength)),
                        actorId,
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength, baseBytesLength).ToArray(),
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength + baseBytesLength, eventBytesLength).ToArray());
                    return true;
                }
                else if (idType == ActorIdType.String)
                {
                    var actorId = Encoding.UTF8.GetString(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength));
                    value = new EventTransUnit(
                        Encoding.UTF8.GetString(bytesSpan.Slice(skipLength, eventTypeLength)),
                        actorId,
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength, baseBytesLength).ToArray(),
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength + baseBytesLength, eventBytesLength).ToArray());
                    return true;
                }
                else if (idType == ActorIdType.Guid)
                {
                    var actorId = Encoding.UTF8.GetString(bytesSpan.Slice(skipLength + eventTypeLength, actorIdBytesLength));
                    value = new EventTransUnit(
                        Encoding.UTF8.GetString(bytesSpan.Slice(skipLength, eventTypeLength)),
                        new Guid(actorId),
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength, baseBytesLength).ToArray(),
                        bytesSpan.Slice(skipLength + eventTypeLength + actorIdBytesLength + baseBytesLength, eventBytesLength).ToArray());
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
