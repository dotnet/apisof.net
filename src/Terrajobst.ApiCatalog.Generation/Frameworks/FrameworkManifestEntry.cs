namespace Terrajobst.ApiCatalog;

public sealed class FrameworkManifestEntry
{
    public FrameworkManifestEntry(string frameworkName,
                                  IReadOnlyList<FrameworkManifestPackage> packages)
    {
        FrameworkName = frameworkName;
        Packages = packages.OrderBy(p => p.Id).ToArray();
    }

    public string FrameworkName { get; }

    public IReadOnlyList<FrameworkManifestPackage> Packages { get; }
}