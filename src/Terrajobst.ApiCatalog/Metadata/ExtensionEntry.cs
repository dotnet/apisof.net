namespace Terrajobst.ApiCatalog
{
    public sealed class ExtensionEntry
    {
        public ExtensionEntry(Guid extendedTypeGuid, Guid extensionMethodGuid)
        {
            ExtendedTypeGuid = extendedTypeGuid;
            ExtensionMethodGuid = extensionMethodGuid;
        }

        public Guid ExtendedTypeGuid { get; }

        public Guid ExtensionMethodGuid { get; }
    }
}