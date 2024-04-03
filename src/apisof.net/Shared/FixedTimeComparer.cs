using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ApisOfDotNet.Shared;

internal static class FixedTimeComparer
{
    public static bool Equals(string x, string y)
    {
        ThrowIfNull(x);
        ThrowIfNull(y);

        var length = int.Max(x.Length, y.Length);
        x = x.PadRight(length);
        y = y.PadRight(length);

        var xBytes = MemoryMarshal.AsBytes(x.AsSpan());
        var yBytes = MemoryMarshal.AsBytes(y.AsSpan());
       
        return CryptographicOperations.FixedTimeEquals(xBytes, yBytes);
    }
}