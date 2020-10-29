using System.Runtime.InteropServices;

namespace Vertex.Utils
{
    public class MurmurHash2
    {
        private const uint M = 0x5bd1e995;
        private const int R = 24;

        public static uint Hash(byte[] data)
        {
            return Hash(data, 0xc58f1a7b);
        }

        public static uint Hash(byte[] data, uint seed)
        {
            int length = data.Length;
            if (length == 0)
            {
                return 0;
            }

            uint h = seed ^ (uint)length;
            int currentIndex = 0;
            uint[] hackArray = new BytetouintConverter { Bytes = data }.UInts;
            while (length >= 4)
            {
                uint k = hackArray[currentIndex++];
                k *= M;
                k ^= k >> R;
                k *= M;

                h *= M;
                h ^= k;
                length -= 4;
            }

            currentIndex *= 4; // fix the length
            switch (length)
            {
                case 3:
                    h ^= (ushort)(data[currentIndex++] | data[currentIndex++] << 8);
                    h ^= (uint)data[currentIndex] << 16;
                    h *= M;
                    break;
                case 2:
                    h ^= (ushort)(data[currentIndex++] | data[currentIndex] << 8);
                    h *= M;
                    break;
                case 1:
                    h ^= data[currentIndex];
                    h *= M;
                    break;
                default:
                    break;
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.
            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;
            return h;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct BytetouintConverter
        {
            [FieldOffset(0)]
            public byte[] Bytes;

            [FieldOffset(0)]
            public uint[] UInts;
        }
    }
}
