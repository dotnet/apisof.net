namespace Terrajobst.ApiCatalog;

public sealed class FrameworkManifestPackage
{
    public FrameworkManifestPackage(string id,
                                    string version,
                                    IReadOnlyList<FrameworkManifestAssembly> assemblies)
    {
        Id = id;
        Version = version;
        Assemblies = assemblies.OrderBy(a => a.Path).ToArray();
    }

    public string Id { get; }

    public string Version { get; }

    public IReadOnlyList<FrameworkManifestAssembly> Assemblies { get; }
}
