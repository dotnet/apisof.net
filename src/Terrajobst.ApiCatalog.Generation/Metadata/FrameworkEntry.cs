namespace Terrajobst.ApiCatalog;

public sealed class FrameworkEntry
{
    public static FrameworkEntry Create(string frameworkName, IReadOnlyList<FrameworkAssemblyEntry> assemblies)
    {
        return new FrameworkEntry(frameworkName, assemblies);
    }

    private FrameworkEntry(string frameworkName, IReadOnlyList<FrameworkAssemblyEntry> assemblies)
    {
        FrameworkName = frameworkName;
        Assemblies = assemblies;
    }

    public string FrameworkName { get; }
    public IReadOnlyList<FrameworkAssemblyEntry> Assemblies { get; }
}