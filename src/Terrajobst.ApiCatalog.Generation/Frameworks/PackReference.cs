namespace Terrajobst.ApiCatalog;

public sealed class PackReference(string name)
{
    public string Name { get; } = name;

    public required string Version { get; init; }

    public required PackKind Kind { get; init; }

    public IReadOnlyList<string> Platforms { get; init; } = [];

    public IReadOnlyList<string> Workloads { get; init; } = [];
}