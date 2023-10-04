using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public class NuGetStore
{
    private readonly NuGetFeed _feed;
    private readonly string _packagesCachePath;
    private readonly Dictionary<string, IReadOnlyList<NuGetVersion>> _packageVersionCache = new();

    public NuGetStore(NuGetFeed feed, string packagesCachePath)
    {
        _feed = feed;
        _packagesCachePath = packagesCachePath;
    }

    public async Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (path is not null && File.Exists(path))
            return new PackageArchiveReader(path);

        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));

        if (path is null)
            return await _feed.GetPackageAsync(identity);

        using (var fileStream = File.Create(path))
            await _feed.CopyPackageStreamAsync(identity, fileStream);
    
        return new PackageArchiveReader(path);
    }

    public async Task<PackageArchiveReader> ResolvePackageAsync(string id, VersionRange range)
    {
        if (!_packageVersionCache.TryGetValue(id, out var versions))
        {
            versions = await _feed.GetAllVersionsAsync(id, includeUnlisted: true);
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
        if (_packagesCachePath is null)
            return null;

        return Path.Combine(_packagesCachePath, $"{id}.{version}.nupkg");
    }
}