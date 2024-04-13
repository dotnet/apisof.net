namespace Terrajobst.UsageCrawling.Collectors;

public sealed class FeatureUsage : UsageMetric
{
    public FeatureUsage(string guidText, string name, string description)
    {
        ThrowIfNullOrEmpty(guidText);
        ThrowIfNullOrEmpty(name);
        ThrowIfNullOrEmpty(description);

        Guid = Guid.Parse(guidText);
        Name = name;
        Description = description;
    }

    public override Guid Guid { get; }
    public string Name { get; }
    public string Description { get; }

    public override string ToString()
    {
        return Name;
    }
}