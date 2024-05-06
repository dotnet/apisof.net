namespace Terrajobst.ApiCatalog;

public sealed class FrameworkAssemblyEntry
{
    public FrameworkAssemblyEntry(string? packName, IReadOnlyList<string> profiles, AssemblyEntry assembly)
    {
        ThrowIfNull(profiles);
        ThrowIfNull(assembly);

        PackName = packName;
        Profiles = profiles;
        Assembly = assembly;
    }

    public string? PackName { get; }
    public IReadOnlyList<string> Profiles { get; }
    public AssemblyEntry Assembly { get; }
}