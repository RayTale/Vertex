using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Vertex.Abstractions.Serialization;

namespace Vertex.Runtime.Serialization
{
    public class DefaultJsonSerializer : ISerializer
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }

        public object Deserialize(ReadOnlySpan<byte> bytes, Type type)
        {
            return JsonSerializer.Deserialize(bytes, type);
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            return JsonSerializer.Deserialize(bytes, type);
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, options);
        }

        public string Serialize(object data, Type type)
        {
            return JsonSerializer.Serialize(data, type, options);
        }

        public byte[] SerializeToUtf8Bytes<T>(T data)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data, data.GetType(), options);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes);
        }

        public byte[] SerializeToUtf8Bytes(object data, Type type)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data, type, options);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type);
        }
    }
}
