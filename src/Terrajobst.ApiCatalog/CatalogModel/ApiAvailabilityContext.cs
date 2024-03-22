using System.Collections.Frozen;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiAvailabilityContext
{
    public static ApiAvailabilityContext Create(ApiCatalogModel catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new ApiAvailabilityContext(catalog);
    }

    private readonly ApiCatalogModel _catalog;
    private readonly FrozenDictionary<NuGetFramework, int> _frameworkIds;
    private readonly FrozenDictionary<int, HashSet<int>> _frameworkAssemblies;
    private readonly FrozenDictionary<int, IReadOnlyList<PackageFolder>> _packageFolders;

    private ApiAvailabilityContext(ApiCatalogModel catalog)
    {
        _catalog = catalog;
        _frameworkAssemblies = catalog.Frameworks.Select(fx => (fx.Id, Assemblies: fx.Assemblies.Select(a => a.Id).ToHashSet()))
                                                 .ToFrozenDictionary(t => t.Id, t => t.Assemblies);

        var frameworkIds = new Dictionary<NuGetFramework, int>();
        var nugetFrameworks = new Dictionary<int, NuGetFramework>();

        foreach (var fx in catalog.Frameworks)
        {
            var nugetFramework = NuGetFramework.Parse(fx.Name);
            if (nugetFramework.IsPCL || fx.Name is "monotouch" or "xamarinios10")
                continue;

            nugetFrameworks.Add(fx.Id, nugetFramework);
            frameworkIds.Add(nugetFramework, fx.Id);
        }

        var packageFolders = new Dictionary<int, IReadOnlyList<PackageFolder>>();

        foreach (var package in catalog.Packages)
        {
            var folders = new Dictionary<NuGetFramework, PackageFolder>();

            foreach (var (framework, assembly) in package.Assemblies)
            {
                if (nugetFrameworks.TryGetValue(framework.Id, out var targetFramework))
                {
                    if (!folders.TryGetValue(targetFramework, out var folder))
                    {
                        folder = new PackageFolder(targetFramework, framework);
                        folders.Add(targetFramework, folder);
                    }

                    folder.Assemblies.Add(assembly);
                }
            }

            if (folders.Count > 0)
                packageFolders.Add(package.Id, folders.Values.ToArray());
        }

        _frameworkIds = frameworkIds.ToFrozenDictionary();
        _packageFolders = packageFolders.ToFrozenDictionary();
    }

    public ApiCatalogModel Catalog => _catalog;

    public ApiAvailability GetAvailability(ApiModel api)
    {
        var result = new List<ApiFrameworkAvailability>();

        foreach (var nugetFramework in _frameworkIds.Keys)
        {
            var availability = GetAvailability(api, nugetFramework);
            if (availability is not null)
                result.Add(availability);
        }

        return new ApiAvailability(result.ToArray());
    }

    public ApiFrameworkAvailability GetAvailability(ApiModel api, NuGetFramework nugetFramework)
    {
        // Try to resolve an in-box assembly

        if (_frameworkIds.TryGetValue(nugetFramework, out var frameworkId))
        {
            if (_frameworkAssemblies.TryGetValue(frameworkId, out var frameworkAssemblies))
            {
                foreach (var declaration in api.Declarations)
                {
                    if (frameworkAssemblies.Contains(declaration.Assembly.Id))
                    {
                        return new ApiFrameworkAvailability(nugetFramework, declaration, null, null);
                    }
                }
            }
        }

        // Try to resolve an assembly in a package for the given framework

        foreach (var declaration in api.Declarations)
        {
            foreach (var (package, _) in declaration.Assembly.Packages)
            {
                if (_packageFolders.TryGetValue(package.Id, out var folders))
                {
                    var folder = NuGetFrameworkUtility.GetNearest(folders, nugetFramework);
                    if (folder is not null && folder.Assemblies.Contains(declaration.Assembly))
                        return new ApiFrameworkAvailability(nugetFramework, declaration, package, folder.TargetFramework);
                }
            }
        }

        return null;
    }

    private sealed class PackageFolder : IFrameworkSpecific
    {
        public PackageFolder(NuGetFramework targetFramework, FrameworkModel framework)
        {
            TargetFramework = targetFramework;
            Framework = framework;
        }

        public NuGetFramework TargetFramework { get; }

        public FrameworkModel Framework { get; }

        public List<AssemblyModel> Assemblies { get; } = new();
    }
}