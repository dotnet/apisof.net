global using static Terrajobst.ApiCatalog.ApiCatalogThrowHelpers;

using System.Runtime.CompilerServices;

namespace Terrajobst.ApiCatalog;

internal static class ApiCatalogThrowHelpers
{
    internal static void ThrowIfRowIndexOutOfRange(int offset, ReadOnlySpan<byte> table, int rowSize, [CallerArgumentExpression(nameof(offset))] string? paramName = null)
    {
        if (offset < 0 ||
            offset >= table.Length ||
            offset % rowSize > 0)
            throw new ArgumentOutOfRangeException($"The offset {offset} is invalid.", offset, paramName);
    }

    internal static void ThrowIfBlobOffsetOutOfRange(int offset, ApiCatalogModel catalog, [CallerArgumentExpression(nameof(offset))] string? paramName = null)
    {
        if (offset < 0 ||
            offset >= catalog.BlobHeap.Length)
            throw new ArgumentOutOfRangeException($"The offset {offset} is invalid.", offset, paramName);
    }

    public static void ThrowIfDefault<T>(T value, [CallerArgumentExpression("value")] string? paramName = null)
        where T: struct, IEquatable<T>
    {
        if (value.Equals(default))
            throw new ArgumentException($"The value representing {typeof(T).Name} must be initialized.", paramName);
    }
}