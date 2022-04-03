using System.IO;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public class NuGetStore
{
    private readonly NuGetFeed _feed;
    private readonly string _packagesCachePath;

    public NuGetStore(NuGetFeed feed, string packagesCachePath)
    {
        _feed = feed;
        _packagesCachePath = packagesCachePath;
    }

    public async Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (path != null && File.Exists(path))
            return new PackageArchiveReader(File.OpenRead(path));

        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));

        if (path == null)
            return await _feed.GetPackageAsync(identity);

        var fileStream = File.Create(path);
        await _feed.CopyPackageStreamAsync(identity, fileStream);
        fileStream.Position = 0;
        return new PackageArchiveReader(fileStream);
    }

    public void DeleteFromCache(string id, string version)
    {
        var path = GetPackagePath(id, version);
        if (path != null)
            File.Delete(path);
    }

    private string GetPackagePath(string id, string version)
    {
        if (_packagesCachePath == null)
            return null;

        return Path.Combine(_packagesCachePath, $"{id}.{version}.nupkg");
    }
}