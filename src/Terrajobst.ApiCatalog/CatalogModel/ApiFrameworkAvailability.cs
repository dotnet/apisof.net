using System.Collections.Immutable;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiFrameworkAvailability
{
    public ApiFrameworkAvailability(NuGetFramework framework,
                                    ImmutableArray<ApiDeclarationModel> frameworkDeclarations,
                                    ImmutableArray<(PackageModel, NuGetFramework, ApiDeclarationModel)> packageDeclarations)
    {
        ThrowIfNull(framework);
        ThrowIfDefault(frameworkDeclarations);
        ThrowIfDefault(packageDeclarations);

        Framework = framework;
        FrameworkDeclarations = frameworkDeclarations;
        PackageDeclarations = packageDeclarations;
    }

    public NuGetFramework Framework { get; }

    public ImmutableArray<ApiDeclarationModel> FrameworkDeclarations { get; }

    public ImmutableArray<(PackageModel Package, NuGetFramework PackageFramework, ApiDeclarationModel Declaration)> PackageDeclarations { get; }

    public bool IsInBox => FrameworkDeclarations.Any();

    public ApiDeclarationModel Declaration => FrameworkDeclarations.Any() ? FrameworkDeclarations.First() : PackageDeclarations.First().Item3;
}