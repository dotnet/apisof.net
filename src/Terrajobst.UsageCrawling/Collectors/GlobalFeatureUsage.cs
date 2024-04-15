using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class GlobalFeatureUsage : UsageMetric
{
    public GlobalFeatureUsage(GlobalFeatureDefinition definition)
    {
        ThrowIfNull(definition);

        Definition = definition;
    }

    public GlobalFeatureDefinition Definition { get; }

    public override Guid Guid => Definition.FeatureId;

    public override string ToString()
    {
        return Definition.Name;
    }
}