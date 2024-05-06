namespace Terrajobst.ApiCatalog;

public sealed class PackageAssemblyEntry
{
    public PackageAssemblyEntry(string framework, AssemblyEntry assembly)
    {
        ThrowIfNullOrEmpty(framework);
        ThrowIfNull(assembly);

        Framework = framework;
        Assembly = assembly;
    }

    public string Framework { get; }
    public AssemblyEntry Assembly { get; }
}
