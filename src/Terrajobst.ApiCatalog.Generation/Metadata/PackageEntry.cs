namespace Terrajobst.ApiCatalog;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, IReadOnlyList<PackageAssemblyEntry> assemblies)
    {
        return new PackageEntry(id, version, assemblies);
    }

    private PackageEntry(string id, string version, IReadOnlyList<PackageAssemblyEntry> assemblies)
    {
        Fingerprint = CatalogExtensions.GetCatalogGuid(id, version);
        Id = id;
        Version = version;
        Assemblies = assemblies;
    }

    public Guid Fingerprint { get; }
    public string Id { get; }
    public string Version { get; }
    public IReadOnlyList<PackageAssemblyEntry> Assemblies { get; }
}