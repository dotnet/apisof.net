using NuGet.Frameworks;
using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling.Collectors;

public readonly struct FeatureUsage : IEquatable<FeatureUsage>
{
    public FeatureUsage(FeatureDefinition feature, Guid featureId, object? argument)
    {
        ThrowIfNull(feature);

        Feature = feature;
        FeatureId = featureId;
        Argument = argument;
    }

    public FeatureDefinition Feature { get; }

    public Guid FeatureId { get; }

    public object? Argument { get; }

    public bool Equals(FeatureUsage other)
    {
        return FeatureId.Equals(other.FeatureId);
    }

    public override bool Equals(object? obj)
    {
        return obj is FeatureUsage other && Equals(other);
    }

    public override int GetHashCode()
    {
        return FeatureId.GetHashCode();
    }

    public static bool operator ==(FeatureUsage left, FeatureUsage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FeatureUsage left, FeatureUsage right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return Argument is null ? Feature.Name : $"{Feature.Name}: {Argument}";
    }

        public static FeatureUsage ForGlobal(GlobalFeatureDefinition feature)
    {
        ThrowIfNull(feature);

        return new FeatureUsage(feature, feature.FeatureId, null);
    }

    public static FeatureUsage ForParameterized<T>(ParameterizedFeatureDefinition<T> feature, T value)
    {
        return ForParameterized(feature, value, value);
    }

    public static FeatureUsage ForParameterized<T>(ParameterizedFeatureDefinition<T> feature, T value, object? argument)
    {
        var featureId = feature.GetFeatureId(value);
        return new FeatureUsage(feature, featureId, argument);
    }

    public static FeatureUsage DefinesAnyRefStructs { get; } = ForGlobal(FeatureDefinition.DefinesAnyRefStructs);

    public static FeatureUsage DefinesAnyDefaultInterfaceMembers { get; } = ForGlobal(FeatureDefinition.DefinesAnyDefaultInterfaceMembers);

    public static FeatureUsage DefinesAnyVirtualStaticInterfaceMembers { get; } = ForGlobal(FeatureDefinition.DefinesAnyVirtualStaticInterfaceMembers);

    public static FeatureUsage UsesNullableReferenceTypes { get; }= ForGlobal(FeatureDefinition.UsesNullableReferenceTypes);

    public static FeatureUsage ForApi(ApiKey api) => ForParameterized(FeatureDefinition.ApiUsage, api.Guid, api.DocumentationId);

    public static FeatureUsage ForApi(string documentationId) => ForApi(new ApiKey(documentationId));

    public static FeatureUsage ForDim(ApiKey baseInterfaceMember) => ForParameterized(FeatureDefinition.DimUsage, baseInterfaceMember.Guid, baseInterfaceMember.DocumentationId);

    public static FeatureUsage ForDim(string documentationId) => ForDim(new ApiKey(documentationId));
}
