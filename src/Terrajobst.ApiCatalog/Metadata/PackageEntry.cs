namespace Terrajobst.ApiCatalog;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        return new PackageEntry(id, version, entries);
    }

    private PackageEntry(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        Fingerprint = CatalogExtensions.GetCatalogGuid(id, version);
        Id = id;
        Version = version;
        Entries = entries;
    }

    public Guid Fingerprint { get; }
    public string Id { get; }
    public string Version { get; }
    public IReadOnlyList<FrameworkEntry> Entries { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }
}