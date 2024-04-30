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
        var latestVersion = versions.Where(v => IsInSameBand(identity, v))
                                    .DefaultIfEmpty()
                                    .Max();

        if (latestVersion is null)
        {
            Console.WriteLine($"warning: Can't resolve {identity}");
            return;
        }

        _dependencies.Enqueue(new PackageDependency(identity.Id, new VersionRange(latestVersion)));
    }

    private static bool IsInSameBand(PackageIdentity identity, NuGetVersion version)
    {
        var components = 4;

        if (identity.Version.Version.Revision <= 0)
        {
            components--;

            if (identity.Version.Version.Build <= 0)
                components--;
        }

        return components switch
        {
            4 => identity.Version.Version.Major == version.Version.Major &&
                 identity.Version.Version.Minor == version.Version.Minor &&
                 identity.Version.Version.Build == version.Version.Build &&
                 identity.Version.Version.Revision == version.Version.Revision,
            3 => identity.Version.Version.Major == version.Version.Major &&
                 identity.Version.Version.Minor == version.Version.Minor &&
                 identity.Version.Version.Build == version.Version.Build,
            2 => identity.Version.Version.Major == version.Version.Major &&
                 identity.Version.Version.Minor == version.Version.Minor,
            _ => false
        };
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