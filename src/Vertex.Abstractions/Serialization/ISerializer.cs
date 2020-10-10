using System;

namespace Vertex.Abstractions.Serialization
{
    public interface ISerializer
    {
        object Deserialize(ReadOnlySpan<byte> bytes, Type type);

        object Deserialize(byte[] bytes, Type type);

        T Deserialize<T>(byte[] bytes);

        T Deserialize<T>(string json);

        object Deserialize(string json, Type type);

        string Serialize<T>(T data);

        string Serialize(object data, Type type);

        byte[] SerializeToUtf8Bytes<T>(T data);

        byte[] SerializeToUtf8Bytes(object data, Type type);
    }
}
