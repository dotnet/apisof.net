using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

/// <summary>
/// This is used to resolve a framework when indexing the contents of a NuGet
/// package for a given framework.
/// </summary>
public abstract class FrameworkLocator
{
    public abstract string[] Locate(NuGetFramework framework);
}