using System.Diagnostics.CodeAnalysis;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiFrameworkAvailability
{
    public ApiFrameworkAvailability(NuGetFramework framework, ApiDeclarationModel declaration, PackageModel? package, NuGetFramework? packageFramework)
    {
        ThrowIfNull(framework);
        
        Framework = framework;
        Declaration = declaration;
        Package = package;
        PackageFramework = packageFramework;
    }

    [MemberNotNullWhen(false, nameof(Package))]
    public bool IsInBox => Package is null;
    public NuGetFramework Framework { get; }
    public ApiDeclarationModel Declaration { get; }
    public PackageModel? Package { get; }
    public NuGetFramework? PackageFramework { get; }
}