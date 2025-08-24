using System.Runtime.CompilerServices;

namespace StringWeaver;

internal static class Helpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOf2(int n)
    {
        n--;

        // Next would be 2^31 = negative
        if (n >= 0x40000000)
        {
            return 0;
        }

        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
}