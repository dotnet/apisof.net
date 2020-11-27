
using NuGet.Frameworks;

namespace ApiCatalog.CatalogModel
{
    public sealed class ApiFrameworkAvailability
    {
        public ApiFrameworkAvailability(NuGetFramework framework, ApiDeclarationModel declaration, PackageModel package, NuGetFramework packageFramework)
        {
            Framework = framework;
            Declaration = declaration;
            Package = package;
            PackageFramework = packageFramework;
        }

        public bool IsInBox => Package == default;
        public NuGetFramework Framework { get; }
        public ApiDeclarationModel Declaration { get; }
        public PackageModel Package { get; }
        public NuGetFramework PackageFramework { get; }
    }
}
