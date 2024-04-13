namespace Terrajobst.UsageCrawling.Collectors;

public sealed class ApiUsage(ApiKey api) : UsageMetric
{
    public override Guid Guid => Api.Guid;

    public ApiKey Api { get; } = api;

    public override string ToString()
    {
        return $"API: {Api.DocumentationId}";
    }
}