using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StringBuffer;

internal static class XxHash32
{
    private const uint PRIME1 = 2654435761U;
    private const uint PRIME2 = 2246822519U;
    private const uint PRIME3 = 3266489917U;
    private const uint PRIME4 = 668265263U;
    private const uint PRIME5 = 374761393U;

    public static unsafe uint Hash(ReadOnlySpan<byte> data)
    {
        unchecked
        {
            var ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));

            var end = ptr + data.Length;
            var hash = (uint)data.Length + PRIME5;

            if (data.Length >= 16)
            {
                var p = (uint*)ptr;
                var limit = (uint*)(end - 16);

                var v1 = PRIME1 + PRIME2;
                var v2 = PRIME2;
                uint v3 = 0;
                var v4 = unchecked(0 - PRIME1);

                do
                {
                    v1 += *p++ * PRIME2; v1 = (v1 << 13) | (v1 >> 19); v1 *= PRIME1;
                    v2 += *p++ * PRIME2; v2 = (v2 << 13) | (v2 >> 19); v2 *= PRIME1;
                    v3 += *p++ * PRIME2; v3 = (v3 << 13) | (v3 >> 19); v3 *= PRIME1;
                    v4 += *p++ * PRIME2; v4 = (v4 << 13) | (v4 >> 19); v4 *= PRIME1;
                } while (p <= limit);

                hash = ((v1 << 1) | (v1 >> 31)) + ((v2 << 7) | (v2 >> 25)) + ((v3 << 12) | (v3 >> 20)) + ((v4 << 18) | (v4 >> 14));

                ptr = (byte*)p;
            }

            while (ptr + 4 <= end)
            {
                hash += *(uint*)ptr * PRIME3;
                hash = ((hash << 17) | (hash >> 15)) * PRIME4;
                ptr += 4;
            }

            while (ptr < end)
            {
                hash += *ptr++ * PRIME5;
                hash = ((hash << 11) | (hash >> 21)) * PRIME1;
            }

            hash ^= hash >> 15;
            hash *= PRIME2;
            hash ^= hash >> 13;
            hash *= PRIME3;
            hash ^= hash >> 16;

            return hash;
        }
    }
}
