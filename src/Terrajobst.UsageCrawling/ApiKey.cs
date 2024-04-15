using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling;

public readonly struct ApiKey : IEquatable<ApiKey>, IComparable<ApiKey>, IComparable
{
    public ApiKey(string documentationId)
    {
        ThrowIfNull(documentationId);

        Guid = FeatureId.Create(documentationId);
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