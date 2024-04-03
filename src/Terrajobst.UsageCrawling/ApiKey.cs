using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Terrajobst.UsageCrawling
{
    public readonly struct ApiKey : IEquatable<ApiKey>, IComparable<ApiKey>, IComparable
    {
        public ApiKey(string documentationId)
        {
            ThrowIfNull(documentationId);

            Guid = ComputeGuid(documentationId);
            DocumentationId = documentationId;
        }

        public Guid Guid { get; }

        public string DocumentationId { get; }

        public bool Equals(ApiKey other)
        {
            return Guid.Equals(other.Guid);
        }

        public override bool Equals(object? obj)
        {
            return obj is ApiKey other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public int CompareTo(ApiKey other)
        {
            return string.CompareOrdinal(DocumentationId, other.DocumentationId);
        }

        public int CompareTo(object? obj)
        {
            if (obj is ApiKey other)
                return CompareTo(other);

            return -1;
        }

        private static Guid ComputeGuid(string documentationId)
        {
            const int maxBytesOnStack = 256;

            var encoding = Encoding.UTF8;
            var maxByteCount = encoding.GetMaxByteCount(documentationId.Length);

            if (maxByteCount <= maxBytesOnStack)
            {
                var buffer = (Span<byte>)stackalloc byte[maxBytesOnStack];
                var written = encoding.GetBytes(documentationId, buffer);
                var utf8Bytes = buffer[..written];
                return HashData(utf8Bytes);
            }
            else
            {
                var utf8Bytes = encoding.GetBytes(documentationId);
                return HashData(utf8Bytes);
            }
        }

        private static Guid HashData(ReadOnlySpan<byte> bytes)
        {
            var hashBytes = (Span<byte>)stackalloc byte[16];
            var written = MD5.HashData(bytes, hashBytes);
            Debug.Assert(written == hashBytes.Length);

            return new Guid(hashBytes);
        }

        public static bool operator ==(ApiKey left, ApiKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ApiKey left, ApiKey right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return DocumentationId;
        }
    }
}