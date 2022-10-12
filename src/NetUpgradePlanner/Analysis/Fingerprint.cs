using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NetUpgradePlanner.Analysis;

internal static class Fingerprint
{
    public static string Create(string text)
    {
        return HashString(text);
    }

    private static string HashString(string text)
    {
        const int maxBytesOnStack = 256;

        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(text.Length);

        if (maxByteCount <= maxBytesOnStack)
        {
            var buffer = (Span<byte>)stackalloc byte[maxBytesOnStack];
            var written = encoding.GetBytes(text, buffer);
            var utf8Bytes = buffer[..written];
            return HashBytes(utf8Bytes);
        }
        else
        {
            var utf8Bytes = encoding.GetBytes(text);
            return HashBytes(utf8Bytes);
        }
    }

    private static string HashBytes(ReadOnlySpan<byte> bytes)
    {
        var hashBytes = (Span<byte>)stackalloc byte[32];
        var written = SHA256.HashData(bytes, hashBytes);
        Debug.Assert(written == hashBytes.Length);

        return Convert.ToHexString(hashBytes);
    }
}