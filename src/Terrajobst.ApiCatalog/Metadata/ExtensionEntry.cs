namespace Terrajobst.ApiCatalog;

public sealed class ExtensionEntry
{
    public ExtensionEntry(Guid fingerprint,
                          Guid extendedTypeGuid,
                          Guid extensionMethodGuid)
    {
        Fingerprint = fingerprint;
        ExtendedTypeGuid = extendedTypeGuid;
        ExtensionMethodGuid = extensionMethodGuid;
    }

    public Guid Fingerprint { get; }

    public Guid ExtendedTypeGuid { get; }

    public Guid ExtensionMethodGuid { get; }
}