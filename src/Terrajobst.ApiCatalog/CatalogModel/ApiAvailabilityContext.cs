using System.Collections.Frozen;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiAvailabilityContext
{
    public static ApiAvailabilityContext Create(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        return new ApiAvailabilityContext(catalog);
    }

    private readonly ApiCatalogModel _catalog;
    private readonly FrozenDictionary<NuGetFramework, int> _frameworkIds;
    private readonly FrozenDictionary<int, FrozenSet<int>> _frameworkAssemblies;
    private readonly FrozenDictionary<int, FrozenDictionary<int, (int PackageId, int FrameworkId)>> _packageAssemblies;

    private ApiAvailabilityContext(ApiCatalogModel catalog)
    {
        _catalog = catalog;
        _frameworkAssemblies = catalog.Frameworks.Select(fx => (fx.Id, Assemblies: fx.Assemblies.Select(a => a.Id).ToFrozenSet()))
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

        // Package assemblies
        
        _frameworkIds = frameworkIds.ToFrozenDictionary();
        
        var packageAssemblies = new Dictionary<int, FrozenDictionary<int, (int PackageId, int FrameworkId)>>();

        foreach (var framework in frameworkIds.Keys)
        {
            var frameworkId = frameworkIds[framework];
            var assemblies = new Dictionary<int, (int, int)>();
            
            foreach (var (packageId, packageFolder) in packageFolders)
            {
                var folder = NuGetFrameworkUtility.GetNearest(packageFolder, framework);
                if (folder is not null)
                {
                    foreach (var assembly in folder.Assemblies)
                    {
                        var packageFrameworkId = frameworkIds[folder.TargetFramework];
                        assemblies.TryAdd(assembly.Id, (packageId, packageFrameworkId));
                    }
                }
            }
            
            packageAssemblies.Add(frameworkId, assemblies.ToFrozenDictionary());
        }

        _packageAssemblies = packageAssemblies.ToFrozenDictionary();
    }

    public ApiCatalogModel Catalog => _catalog;

    public bool IsAvailable(ApiModel api, NuGetFramework framework)
    {
        var frameworkId = _frameworkIds[framework];
        var frameworkAssemblies = _frameworkAssemblies[frameworkId];
        var packageAssemblies = _packageAssemblies[frameworkId];

        foreach (var declaration in api.Declarations)
        {
            var assemblyId = declaration.Assembly.Id;
            if (frameworkAssemblies.Contains(assemblyId) || packageAssemblies.ContainsKey(assemblyId))
                return true;
        }

        return false;
    }

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
        if (_frameworkIds.TryGetValue(nugetFramework, out var frameworkId))
        {
            // Try to resolve an in-box assembly

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

            // Try to resolve an assembly in a package for the given framework

            if (_packageAssemblies.TryGetValue(frameworkId, out var packageAssemblies))
            {
                foreach (var declaration in api.Declarations)
                {
                    if (packageAssemblies.TryGetValue(declaration.Assembly.Id, out var packageInfo))
                    {
                        var package = new PackageModel(_catalog, packageInfo.PackageId);
                        var packageFramework = new FrameworkModel(_catalog, packageInfo.FrameworkId);
                        var nugetPackageFramework = NuGetFramework.Parse(packageFramework.Name);
                        return new ApiFrameworkAvailability(nugetFramework, declaration, package, nugetPackageFramework);
                    }
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