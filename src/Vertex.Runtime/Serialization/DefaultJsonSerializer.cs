using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Vertex.Abstractions.Serialization;

namespace Vertex.Runtime.Serialization
{
    public class DefaultJsonSerializer : ISerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public object Deserialize(ReadOnlySpan<byte> bytes, Type type)
        {
            return JsonSerializer.Deserialize(bytes, type, Options);
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            return JsonSerializer.Deserialize(bytes, type, Options);
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, Options);
        }

        public string Serialize(object data, Type type)
        {
            return JsonSerializer.Serialize(data, type, Options);
        }

        public byte[] SerializeToUtf8Bytes<T>(T data)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data, data.GetType(), Options);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }

        public byte[] SerializeToUtf8Bytes(object data, Type type)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data, type, Options);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, Options);
        }
    }
}
