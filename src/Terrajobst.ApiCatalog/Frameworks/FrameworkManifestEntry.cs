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

    // TODO: It seems here is where we want to introduce the notion of a package group with pre-requisites, such as:
    //
    // - UseXxx properties
    // - One of the workloads A, B, or C installed
    public IReadOnlyList<FrameworkManifestPackage> Packages { get; }
}