using System;
using System.Text;
using Xunit;

namespace Vertex.Utils.Test.Buffer
{
    public class SharedArray_Test
    {
        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        public void Rent(int length)
        {
            using var buffer = SharedArray.Rent(length);
            Assert.True(buffer.Position == 0);
            Assert.True(buffer.Length % 4096 == 0);
            buffer.Write(10);
            Assert.Equal(sizeof(int), buffer.AsSpan().Length);
            Assert.Equal(sizeof(int), buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteInt(int count)
        {
            using var buffer = SharedArray.Rent();
            for (int i = 0; i < count; i++)
            {
                buffer.Write(i);
            }

            Assert.Equal(sizeof(int) * count, buffer.AsSpan().Length);
            Assert.Equal(sizeof(int) * count, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteUInt(int count)
        {
            using var buffer = SharedArray.Rent();
            for (int i = 0; i < count; i++)
            {
                buffer.Write((uint)i);
            }

            Assert.Equal(sizeof(uint) * count, buffer.AsSpan().Length);
            Assert.Equal(sizeof(uint) * count, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteLong(int count)
        {
            using var buffer = SharedArray.Rent();
            for (int i = 0; i < count; i++)
            {
                buffer.Write((long)i);
            }

            Assert.Equal(sizeof(long) * count, buffer.AsSpan().Length);
            Assert.Equal(sizeof(long) * count, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteShort(int count)
        {
            using var buffer = SharedArray.Rent();
            for (int i = 0; i < count; i++)
            {
                buffer.Write((short)i);
            }

            Assert.Equal(sizeof(short) * count, buffer.AsSpan().Length);
            Assert.Equal(sizeof(short) * count, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteUShort(int count)
        {
            using var buffer = SharedArray.Rent();
            for (int i = 0; i < count; i++)
            {
                buffer.Write((ushort)i);
            }

            Assert.Equal(sizeof(ushort) * count, buffer.AsSpan().Length);
            Assert.Equal(sizeof(ushort) * count, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteBytes(int count)
        {
            using var buffer = SharedArray.Rent();
            var length = 0;
            for (int i = 0; i < count; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(i.ToString());
                length += bytes.Length;
                buffer.Write(bytes);
            }

            Assert.Equal(length, buffer.AsSpan().Length);
            Assert.Equal(length, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteString(int count)
        {
            using var buffer = SharedArray.Rent();
            var length = 0;
            for (int i = 0; i < count; i++)
            {
                buffer.WriteUtf8String(i.ToString());
                length += Encoding.UTF8.GetByteCount(i.ToString());
            }

            Assert.Equal(length, buffer.AsSpan().Length);
            Assert.Equal(length, buffer.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        public void WriteChars(int count)
        {
            using var buffer = SharedArray.Rent();
            var length = 0;
            for (int i = 0; i < count; i++)
            {
                buffer.WriteUtf8String(i.ToString().AsSpan());
                length += Encoding.UTF8.GetByteCount(i.ToString());
            }

            Assert.Equal(length, buffer.AsSpan().Length);
            Assert.Equal(length, buffer.ToArray().Length);
        }
    }
}
