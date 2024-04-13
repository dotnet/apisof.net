namespace Terrajobst.UsageCrawling.Collectors;

public sealed class VersionedFeatureSet
{
    public VersionedFeatureSet(int version, IReadOnlySet<Guid> features)
    {
        ThrowIfNegative(version);
        ThrowIfNull(features);

        Version = version;
        Features = features;
    }

    public int Version { get; }

    public IReadOnlySet<Guid> Features { get; }
}