using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    /// <summary>
    /// This is used to resolve a framework when indexing the contents of a NuGet
    /// package for a given framework.
    /// </summary>
    internal abstract class FrameworkLocator
    {
        public abstract Task<FileSet> LocateAsync(NuGetFramework framework);
    }
}
