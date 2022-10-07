namespace Terrajobst.ApiCatalog;

public sealed class MissingNetFxFeature
{
    public MissingNetFxFeature(string name,
                               string description,
                               string url,
                               IEnumerable<ApiMatcher> appliesTo)
    {
        Name = name;
        Description = description;
        Url = url;
        AppliesTo = appliesTo.ToArray();
    }

    public string Name { get; }
    public string Description { get; }
    public string Url { get; }
    public IReadOnlyList<ApiMatcher> AppliesTo { get; }
}
