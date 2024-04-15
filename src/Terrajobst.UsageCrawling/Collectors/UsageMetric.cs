using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling.Collectors;

public abstract class UsageMetric : IEquatable<UsageMetric>
{
    public abstract Guid Guid { get; }

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

    public static GlobalFeatureUsage DefinesAnyRefStructs { get; } = new(FeatureDefinition.DefinesAnyRefStructs);

    public static GlobalFeatureUsage DefinesAnyDefaultInterfaceMembers { get; } = new(FeatureDefinition.DefinesAnyDefaultInterfaceMembers);

    public static GlobalFeatureUsage DefinesAnyVirtualStaticInterfaceMembers { get; } = new(FeatureDefinition.DefinesAnyVirtualStaticInterfaceMembers);

    public static ApiFeatureUsage ForApi(ApiKey api) => new(FeatureDefinition.ApiUsage, api);

    public static ApiFeatureUsage ForApi(string documentationId) => ForApi(new ApiKey(documentationId));

    public static ApiFeatureUsage ForDefaultInterfaceImplementation(ApiKey baseInterfaceMember) => new(FeatureDefinition.DimUsage, baseInterfaceMember);

    public static ApiFeatureUsage ForDefaultInterfaceImplementation(string documentationId) => ForDefaultInterfaceImplementation(new ApiKey(documentationId));
}
