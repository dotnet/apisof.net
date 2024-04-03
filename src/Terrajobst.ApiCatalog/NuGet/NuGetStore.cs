using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public class NuGetStore
{
    private readonly NuGetFeed[] _feeds;
    private readonly Dictionary<string, IReadOnlyList<NuGetVersion>> _packageVersionCache = new();

    public NuGetStore(string packagesCachePath, params NuGetFeed[] feeds)
    {
        ThrowIfNullOrEmpty(packagesCachePath);
        ThrowIfNull(feeds);

        if (feeds.Length == 0)
            throw new ArgumentException("must have at least one feed", nameof(feeds));

        PackagesCachePath = packagesCachePath;
        _feeds = feeds;
    }

    public string PackagesCachePath { get; }

    public Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        return GetPackageAsync(identity.Id, identity.Version.ToNormalizedString());
    }

    public async Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (File.Exists(path))
            return new PackageArchiveReader(path);

        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using (var fileStream = File.Create(path))
        {
            var success = false;

            foreach (var feed in _feeds)
            {
                if (await feed.TryCopyPackageStreamAsync(identity, fileStream))
                {
                    success = true;
                    break;
                }
            }

            if (!success)
                throw new Exception($"Can't resolve package {id} {version}");
        }

        return new PackageArchiveReader(path);
    }

    public async Task<PackageArchiveReader> ResolvePackageAsync(string id, VersionRange range)
    {
        var versions = await GetVersionsAsync(id);

        var resolvedVersion = versions.FindBestMatch(range, x => x);
        if (resolvedVersion is null)
            return null;

        if (resolvedVersion != range.MinVersion)
            Console.WriteLine($"Resolved {id} {range} -> {resolvedVersion}");

        return await GetPackageAsync(id, resolvedVersion.ToNormalizedString());
    }

    public async Task<IReadOnlyList<NuGetVersion>> GetVersionsAsync(string id)
    {
        if (!_packageVersionCache.TryGetValue(id, out var versions))
        {
            var allVersions = new SortedSet<NuGetVersion>();

            foreach (var feed in _feeds)
            {
                var feedVersions = await feed.GetAllVersionsAsync(id, includeUnlisted: true);
                allVersions.UnionWith(feedVersions);
            }

            versions = allVersions.ToArray();

            _packageVersionCache.Add(id, versions);
        }

        return versions;
    }

    public void DeleteFromCache(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (path is not null)
            File.Delete(path);
    }

    private string GetPackagePath(string id, string version)
    {
        return Path.Combine(PackagesCachePath, $"{id}.{version}.nupkg");
    }
}