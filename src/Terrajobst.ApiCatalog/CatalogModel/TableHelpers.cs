using System.Buffers.Binary;

namespace Terrajobst.ApiCatalog;

internal static class TableHelpers
{
    public static byte ReadByte(this ReadOnlySpan<byte> table, int offset)
    {
        return table[offset];
    }

    public static int ReadInt32(this ReadOnlySpan<byte> table, int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(table[offset..]);
    }

    public static float ReadSingle(this ReadOnlySpan<byte> table, int offset)
    {
        return BinaryPrimitives.ReadSingleLittleEndian(table[offset..]);
    }

    public static Guid ReadGuid(this ReadOnlySpan<byte> table, int offset)
    {
        var guidSpan = table.Slice(offset, 16);
        return new Guid(guidSpan);
    }
}