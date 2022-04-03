using System.Collections.Generic;
using System.Linq;

using NuGet.Frameworks;

namespace ApiCatalog.CatalogModel;

public sealed class ApiAvailability
{
    public ApiAvailability(IEnumerable<ApiFrameworkAvailability> frameworks)
    {
        Frameworks = frameworks.ToArray();
    }

    public IReadOnlyList<ApiFrameworkAvailability> Frameworks { get; }

    public static ApiAvailability Create(ApiModel api)
    {
        // Index declarations by framework

        var frameworkDeclarations = new Dictionary<FrameworkModel, List<ApiDeclarationModel>>();
        var packagedDeclarations = new Dictionary<PackageModel, Dictionary<FrameworkModel, PackageFolder>>();

        foreach (var declaration in api.Declarations)
        {
            foreach (var framework in declaration.Assembly.Frameworks)
            {
                if (!frameworkDeclarations.TryGetValue(framework, out var assemblies))
                {
                    assemblies = new List<ApiDeclarationModel>();
                    frameworkDeclarations.Add(framework, assemblies);
                }

                assemblies.Add(declaration);
            }

            foreach (var (package, framework) in declaration.Assembly.Packages)
            {
                if (!packagedDeclarations.TryGetValue(package, out var packageFolders))
                {
                    packageFolders = new Dictionary<FrameworkModel, PackageFolder>();
                    packagedDeclarations.Add(package, packageFolders);
                }

                if (!packageFolders.TryGetValue(framework, out var packageFolder))
                {
                    packageFolder = new PackageFolder(framework);
                    packageFolders.Add(framework, packageFolder);
                }

                packageFolder.Declarations.Add(declaration);
            }
        }

        // Compute visibility

        var frameworkAvailabilities = new List<ApiFrameworkAvailability>();

        foreach (var fx in api.Catalog.Frameworks)
        {
            var nugetFramework = NuGetFramework.ParseFolder(fx.Name);
            if (nugetFramework.IsPCL || fx.Name == "monotouch" || fx.Name == "xamarinios10")
                continue;

            if (frameworkDeclarations.TryGetValue(fx, out var declarations))
            {
                foreach (var declaration in declarations)
                    frameworkAvailabilities.Add(new ApiFrameworkAvailability(nugetFramework, declaration, default, default));
            }
            else
            {
                foreach (var (package, packageFolders) in packagedDeclarations)
                {
                    var folder = packageFolders.Values.GetNearest(nugetFramework);
                    if (folder != null)
                    {
                        foreach (var declaration in folder.Declarations)
                            frameworkAvailabilities.Add(new ApiFrameworkAvailability(nugetFramework, declaration, package, folder.TargetFramework));
                    }
                }
            }
        }

        return new ApiAvailability(frameworkAvailabilities);
    }

    private sealed class PackageFolder : IFrameworkSpecific
    {
        public PackageFolder(FrameworkModel framework)
        {
            TargetFramework = NuGetFramework.ParseFolder(framework.Name);
            Framework = framework;
        }

        public NuGetFramework TargetFramework { get; }
        public FrameworkModel Framework { get; }
        public List<ApiDeclarationModel> Declarations { get; } = new();
    }
}