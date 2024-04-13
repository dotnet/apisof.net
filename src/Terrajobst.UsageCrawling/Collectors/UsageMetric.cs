using System.Security.Cryptography;

namespace Terrajobst.UsageCrawling.Collectors;

public abstract class UsageMetric : IEquatable<UsageMetric>
{
    public abstract Guid Guid { get; }

    private static Guid CreateGuid(Guid g1, Guid g2)
    {
        var bytes = (Span<byte>) stackalloc byte[32];
        g1.TryWriteBytes(bytes);
        g2.TryWriteBytes(bytes[16..]);

        var hashBytes = (Span<byte>)stackalloc byte[16];
        MD5.HashData(bytes, hashBytes);
        return new Guid(hashBytes);
    }

    public bool Equals(UsageMetric? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Guid.Equals(other.Guid);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((UsageMetric)obj);
    }

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    public static bool operator ==(UsageMetric? left, UsageMetric? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UsageMetric? left, UsageMetric? right)
    {
        return !Equals(left, right);
    }

    public static FeatureUsage DefinesAnyRefStructs { get; } =
        new("740841a3-5c09-426a-b43b-750d21250c01", nameof(DefinesAnyRefStructs),
            "Indicates whether the user defined any ref structs. This doesn't include usage of existing ref structs, like Span<T>.");

    public static FeatureUsage DefinesAnyDefaultInterfaceMembers { get; } =
        new("745807b1-d30a-405c-aa91-209bae5f5ea9", nameof(DefinesAnyDefaultInterfaceMembers),
            "Indicates whether the user defined any default interface members. This doesn't include usage of statics, virtual or non-virtual.");

    public static FeatureUsage DefinesAnyVirtualStaticInterfaceMembers { get; } =
        new("580c614a-45e8-4f91-a007-322377dd23a9", nameof(DefinesAnyVirtualStaticInterfaceMembers),
            "Indicates whether the user defined any virtual static interface members. This doesn't include non-virtual statics.");

    public static ApiUsage ForApi(ApiKey api) => new(api);

    public static ApiUsage ForApi(string documentationId) => new(new ApiKey(documentationId));

    public static DimUsage ForDefaultInterfaceImplementation(ApiKey baseInterfaceMember) => new(baseInterfaceMember);

    public static DimUsage ForDefaultInterfaceImplementation(string documentationId) => new(new ApiKey(documentationId));

    public static Guid CreateGuidForDimUsage(Guid baseInterfaceMember)
    {
        return CreateGuid(DefinesAnyDefaultInterfaceMembers.Guid, baseInterfaceMember);
    }
}