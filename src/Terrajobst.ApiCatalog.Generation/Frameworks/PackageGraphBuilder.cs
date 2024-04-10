using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

internal sealed class PackageGraphBuilder
{
    private readonly NuGetStore _store;
    private readonly NuGetFramework _framework;

    private readonly Dictionary<string, NuGetVersion> _resolvedVersions = new();
    private readonly Queue<PackageDependency> _dependencies = new();

    public PackageGraphBuilder(NuGetStore store, NuGetFramework framework)
    {
        _store = store;
        _framework = framework;
    }

    public async Task EnqueueAsync(PackageIdentity identity)
    {
        var versions = await _store.GetVersionsAsync(identity.Id);
        var latestVersion = versions.Where(v => v.Major == identity.Version.Major &&
                                                v.Minor == identity.Version.Minor)
            .DefaultIfEmpty()
            .Max();

        if (latestVersion is null)
        {
            Console.WriteLine($"warning: Can't resolve {identity}");
            return;
        }

        _dependencies.Enqueue(new PackageDependency(identity.Id, new VersionRange(latestVersion)));
    }

    public async Task<IReadOnlyList<PackageIdentity>> BuildAsync()
    {
        while (_dependencies.Count > 0)
        {
            var dependency = _dependencies.Dequeue();

            if (_resolvedVersions.TryGetValue(dependency.Id, out var dependencyVersion) &&
                dependency.VersionRange.Satisfies(dependencyVersion))
            {
                continue;
            }

            var allVersions = await _store.GetVersionsAsync(dependency.Id);
            dependencyVersion = dependency.VersionRange.FindBestMatch(allVersions);

            if (dependencyVersion is null)
            {
                Console.WriteLine($"warning: Can't find a best match for {dependency.Id}: {dependency.VersionRange}");
                Console.WriteLine("Available versions:");
                foreach (var v in allVersions)
                    Console.WriteLine(v);
                continue;
            }

            _resolvedVersions[dependency.Id] = dependencyVersion;

            var dependencyIdentity = new PackageIdentity(dependency.Id, dependencyVersion);

            using var package = await _store.GetPackageAsync(dependencyIdentity);
            var dependencies = package.GetPackageDependencies();

            var dependencyGroup = NuGetFrameworkUtility.GetNearest(dependencies, _framework);
            if (dependencyGroup is not null)
            {
                foreach (var d in dependencyGroup.Packages)
                    _dependencies.Enqueue(d);
            }
        }

        return _resolvedVersions.Select(kv => new PackageIdentity(kv.Key, kv.Value))
                                .ToArray();
    }
}