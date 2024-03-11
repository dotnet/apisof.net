using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public class NuGetStore
{
    private readonly string _packagesCachePath;
    private readonly NuGetFeed[] _feeds;
    private readonly Dictionary<string, IReadOnlyList<NuGetVersion>> _packageVersionCache = new();

    public NuGetStore(string packagesCachePath, params NuGetFeed[] feeds)
    {
        ArgumentException.ThrowIfNullOrEmpty(packagesCachePath);
        ArgumentNullException.ThrowIfNull(feeds);

        if (feeds.Length == 0)
            throw new ArgumentException("must have at least one feed", nameof(feeds));
        
        _packagesCachePath = packagesCachePath;
        _feeds = feeds;
    }

    public async Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (File.Exists(path))
            return new PackageArchiveReader(path);

        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));

        await using (var fileStream = File.Create(path))
        {
            var success = false;

            foreach (var feed in _feeds)
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    await feed.CopyPackageStreamAsync(identity, memoryStream);
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(fileStream);
                    success = true;
                    break;
                }
                catch
                {
                    // Try next feed
                }
            }

            if (!success)
                throw new Exception($"Can't resolve package {id} {version}");
        }
    
        return new PackageArchiveReader(path);
    }

    public async Task<PackageArchiveReader> ResolvePackageAsync(string id, VersionRange range)
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

        var resolvedVersion = versions.FindBestMatch(range, x => x);
        if (resolvedVersion is null)
            return null;

        if (resolvedVersion != range.MinVersion)
            Console.WriteLine($"Resolved {id} {range} -> {resolvedVersion}");

        return await GetPackageAsync(id, resolvedVersion.ToNormalizedString());
    }

    public void DeleteFromCache(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (path is not null)
            File.Delete(path);
    }

    private string GetPackagePath(string id, string version)
    {
        return Path.Combine(_packagesCachePath, $"{id}.{version}.nupkg");
    }
}