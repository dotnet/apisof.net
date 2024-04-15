using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class ApiFeatureUsage : UsageMetric
{
    public ApiFeatureUsage(ApiFeatureDefinition definition, ApiKey api)
    {
        ThrowIfNull(definition);

        Definition = definition;
        Api = api;
        Guid = definition.GetFeatureId(api.Guid);
    }

    public ApiFeatureDefinition Definition { get; }

    public ApiKey Api { get; }

    public override Guid Guid { get; }

    public override string ToString()
    {
        return $"{Definition.Name}: {Api.DocumentationId}";
    }
}