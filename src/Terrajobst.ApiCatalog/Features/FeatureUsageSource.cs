namespace Terrajobst.ApiCatalog.Features;

public sealed class FeatureUsageSource
{
    public FeatureUsageSource(string name, DateOnly date)
    {
        ThrowIfNullOrEmpty(name);

        Name = name;
        Date = date;
    }

    public string Name { get; }

    public DateOnly Date { get; }
}