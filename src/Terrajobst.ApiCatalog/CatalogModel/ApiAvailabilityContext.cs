using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;

using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

internal sealed class ApiAvailabilityContext
{
    private readonly ApiCatalogModel _catalog;
    private readonly FrozenDictionary<int, NuGetFramework> _frameworkById;
    private readonly FrozenDictionary<NuGetFramework, int> _frameworkIds;
    private readonly FrozenDictionary<int, FrozenSet<int>> _frameworkAssemblies;
    private readonly FrozenDictionary<int, FrozenDictionary<int, (int PackageId, int FrameworkId)>> _packageAssemblies;

    public ApiAvailabilityContext(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        _catalog = catalog;
        _frameworkById = catalog.Frameworks.ToFrozenDictionary(fx => fx.Id, fx => NuGetFramework.Parse(fx.Name));
        _frameworkAssemblies = catalog.Frameworks.Select(fx => (fx.Id, Assemblies: fx.Assemblies.Select(a => a.Id).ToFrozenSet()))
                                                 .ToFrozenDictionary(t => t.Id, t => t.Assemblies);

        var frameworkIds = new Dictionary<NuGetFramework, int>();
        var nugetFrameworks = new Dictionary<int, NuGetFramework>();

        foreach (var fx in catalog.Frameworks)
        {
            var nugetFramework = _frameworkById[fx.Id];
            nugetFrameworks.Add(fx.Id, nugetFramework);
            frameworkIds.Add(nugetFramework, fx.Id);
        }

        var packageFolders = new ConcurrentDictionary<int, IReadOnlyList<PackageFolder>>();

        Parallel.ForEach(catalog.Packages, package =>
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
                packageFolders[package.Id] = folders.Values.ToArray();
        });

        // Package assemblies

        _frameworkIds = frameworkIds.ToFrozenDictionary();

        var packageAssemblies = new ConcurrentDictionary<int, FrozenDictionary<int, (int PackageId, int FrameworkId)>>();

        Parallel.ForEach(frameworkIds.Keys, framework =>
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

            packageAssemblies[frameworkId] = assemblies.ToFrozenDictionary();
        });

        _packageAssemblies = packageAssemblies.ToFrozenDictionary();
    }

    public NuGetFramework GetNuGetFramework(int frameworkId) => _frameworkById[frameworkId];

    public FrameworkModel? GetFramework(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        if (_frameworkIds.TryGetValue(framework, out var id))
            return new FrameworkModel(_catalog, id);

        return null;
    }

    public bool IsAvailable(ApiModel api, NuGetFramework framework)
    {
        ThrowIfDefault(api);
        ThrowIfNull(framework);

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

    public ApiDeclarationModel? GetDefinition(ApiModel api, NuGetFramework framework)
    {
        ThrowIfDefault(api);
        ThrowIfNull(framework);

        var frameworkId = _frameworkIds[framework];
        var frameworkAssemblies = _frameworkAssemblies[frameworkId];

        foreach (var declaration in api.Declarations)
        {
            var assemblyId = declaration.Assembly.Id;
            if (frameworkAssemblies.Contains(assemblyId))
                return declaration;
        }

        return null;
    }

    public ApiAvailability GetAvailability(ApiModel api)
    {
        ThrowIfDefault(api);

        var result = new List<ApiFrameworkAvailability>();

        foreach (var nugetFramework in _frameworkIds.Keys)
        {
            var availability = GetAvailability(api, nugetFramework);
            if (availability is not null)
                result.Add(availability);
        }

        return new ApiAvailability(result.ToArray());
    }

    public ApiFrameworkAvailability? GetAvailability(ApiModel api, NuGetFramework framework)
    {
        ThrowIfDefault(api);
        ThrowIfNull(framework);

        if (!_frameworkIds.TryGetValue(framework, out var frameworkId))
            return null;

        // Try to resolve an in-box assembly

        if (!_frameworkAssemblies.TryGetValue(frameworkId, out var frameworkAssemblies))
            frameworkAssemblies = FrozenSet<int>.Empty;

        // Try to resolve an assembly in a package for the given framework

        if (!_packageAssemblies.TryGetValue(frameworkId, out var packageAssemblies))
            packageAssemblies = FrozenDictionary<int, (int PackageId, int FrameworkId)>.Empty;

        ImmutableArray<ApiDeclarationModel>.Builder? frameworkBuilder = null;
        ImmutableArray<(PackageModel, NuGetFramework, ApiDeclarationModel)>.Builder? packageBuilder = null;

        foreach (var declaration in api.Declarations)
        {
            if (frameworkAssemblies.Contains(declaration.Assembly.Id))
            {
                frameworkBuilder ??= ImmutableArray.CreateBuilder<ApiDeclarationModel>();
                frameworkBuilder.Add(declaration);
            }

            if (packageAssemblies.TryGetValue(declaration.Assembly.Id, out var packageInfo))
            {
                packageBuilder ??= ImmutableArray.CreateBuilder<(PackageModel, NuGetFramework, ApiDeclarationModel)>();

                var package = new PackageModel(_catalog, packageInfo.PackageId);
                var packageFramework = new FrameworkModel(_catalog, packageInfo.FrameworkId).NuGetFramework;
                packageBuilder.Add((package, packageFramework, declaration));
            }
        }

        if (frameworkBuilder is null && packageBuilder is null)
            return null;

        var frameworks = frameworkBuilder?.ToImmutable() ?? ImmutableArray<ApiDeclarationModel>.Empty;
        var packages = packageBuilder?.ToImmutable() ?? ImmutableArray<(PackageModel, NuGetFramework, ApiDeclarationModel)>.Empty;

        return new ApiFrameworkAvailability(framework, frameworks, packages);
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