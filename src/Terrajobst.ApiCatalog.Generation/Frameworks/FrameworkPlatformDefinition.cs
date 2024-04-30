namespace Terrajobst.ApiCatalog;

public sealed class FrameworkPlatformDefinition
{
    public FrameworkPlatformDefinition(string name)
    {
        ThrowIfNullOrEmpty(name);

        Name = name;
    }

    public string Name { get; }

    public required IReadOnlyList<string> Versions { get; init; }
}