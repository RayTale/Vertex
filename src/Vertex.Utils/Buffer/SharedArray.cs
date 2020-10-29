using System;
using System.Buffers;
using System.Text;

namespace Vertex.Utils
{
    public class SharedArray : IDisposable
    {
        private const int PageSize = 4096;

        public SharedArray(int minSize)
        {
            this.CurrentBuffer = ArrayPool<byte>.Shared.Rent(((minSize / PageSize) + 1) * PageSize);
        }

        public byte[] CurrentBuffer { get; private set; }

        public int Length => this.CurrentBuffer.Length;

        public int Position { get; set; }

        public static SharedArray Rent(int minSize = 0)
        {
            return new SharedArray(minSize);
        }

        /// <summary>
        /// write data to stream.
        /// </summary>
        /// <remarks>if stream data length is over int.MaxValue, this method throws IndexOutOfRangeException.</remarks>
        /// <param name="buffer">buffer.</param>
        /// <param name="offset">offset.</param>
        /// <param name="count">count.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            var endOffset = this.Length + count;
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            Buffer.BlockCopy(buffer, offset, this.CurrentBuffer, this.Position, count);
            this.Position += count;
        }

        public void Write(byte @byte)
        {
            var endOffset = this.Length + 1;
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            this.CurrentBuffer[this.Position] = @byte;
            this.Position += 1;
        }

        public void Write(Span<byte> span)
        {
            var endOffset = this.Length + span.Length;
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            span.CopyTo(this.CurrentBuffer.AsSpan(this.Position));
            this.Position += span.Length;
        }

        public void WriteUtf8String(ReadOnlySpan<char> chars)
        {
            try
            {
                var count = Encoding.UTF8.GetBytes(chars, this.CurrentBuffer.AsSpan(this.Position));
                this.Position += count;
            }
            catch (ArgumentException)
            {
                this.ReallocateBuffer(this.Length + PageSize);
                this.WriteUtf8String(chars);
            }
        }

        public void Write(long number)
        {
            var endOffset = this.Length + sizeof(long);
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            BitConverter.TryWriteBytes(this.CurrentBuffer.AsSpan(this.Position), number);
            this.Position += sizeof(long);
        }

        public void Write(int number)
        {
            var endOffset = this.Length + sizeof(int);
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            BitConverter.TryWriteBytes(this.CurrentBuffer.AsSpan(this.Position), number);
            this.Position += sizeof(int);
        }

        public void Write(uint number)
        {
            var endOffset = this.Length + sizeof(uint);
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            BitConverter.TryWriteBytes(this.CurrentBuffer.AsSpan(this.Position), number);
            this.Position += sizeof(uint);
        }

        public void Write(short number)
        {
            var endOffset = this.Length + sizeof(short);
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            BitConverter.TryWriteBytes(this.CurrentBuffer.AsSpan(this.Position), number);
            this.Position += sizeof(short);
        }

        public void Write(ushort number)
        {
            var endOffset = this.Length + sizeof(ushort);
            if (endOffset > this.CurrentBuffer.Length)
            {
                this.ReallocateBuffer(endOffset + PageSize);
            }

            BitConverter.TryWriteBytes(this.CurrentBuffer.AsSpan(this.Position), number);
            this.Position += sizeof(ushort);
        }

        public void Write(Span<char> chars)
        {
            try
            {
                var count = Encoding.UTF8.GetBytes(chars, this.CurrentBuffer.AsSpan(this.Position));
                this.Position += count;
            }
            catch (ArgumentException)
            {
                this.ReallocateBuffer(this.Length + PageSize);
                this.WriteUtf8String(chars);
            }
        }

        public Span<byte> AsSpan() => this.CurrentBuffer.AsSpan(0, this.Position);

        public byte[] ToArray() => this.CurrentBuffer[0..this.Position];

        public void Dispose() => ArrayPool<byte>.Shared.Return(this.CurrentBuffer);

        private void ReallocateBuffer(int minimumRequired)
        {
            var tmp = ArrayPool<byte>.Shared.Rent(minimumRequired);
            Buffer.BlockCopy(this.CurrentBuffer, 0, tmp, 0, this.Position);
            ArrayPool<byte>.Shared.Return(this.CurrentBuffer);
            this.CurrentBuffer = tmp;
        }
    }
}
