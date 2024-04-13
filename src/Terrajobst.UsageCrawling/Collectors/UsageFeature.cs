namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DimUsage(ApiKey baseInterfaceMember) : UsageMetric
{
    public override Guid Guid { get; } = CreateGuidForDimUsage(baseInterfaceMember.Guid);

    public ApiKey BaseInterfaceMember { get; } = baseInterfaceMember;

    public override string ToString()
    {
        return $"DIM: {BaseInterfaceMember.DocumentationId}";
    }
}
