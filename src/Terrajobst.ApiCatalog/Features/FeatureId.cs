using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Terrajobst.ApiCatalog.Features;

public static class FeatureId
{
    public static Guid Create(Guid g1, Guid g2)
    {
        var bytes = (Span<byte>) stackalloc byte[32];
        g1.TryWriteBytes(bytes);
        g2.TryWriteBytes(bytes[16..]);
        return Create(bytes);
    }

    public static Guid Create(Guid guid, string text)
    {
        ThrowIfNull(text);

        var guidText = Create(text);
        return Create(guid, guidText);
    }

    public static Guid Create(string text)
    {
        ThrowIfNull(text);

        const int maxBytesOnStack = 256;

        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(text.Length);

        if (maxByteCount <= maxBytesOnStack)
        {
            var buffer = (Span<byte>)stackalloc byte[maxBytesOnStack];
            var written = encoding.GetBytes(text, buffer);
            var utf8Bytes = buffer[..written];
            return Create(utf8Bytes);
        }
        else
        {
            var utf8Bytes = encoding.GetBytes(text);
            return Create(utf8Bytes);
        }
    }

    private static Guid Create(ReadOnlySpan<byte> bytes)
    {
        var hashBytes = (Span<byte>)stackalloc byte[16];
        var written = MD5.HashData(bytes, hashBytes);
        Debug.Assert(written == hashBytes.Length);

        return new Guid(hashBytes);
    }
}