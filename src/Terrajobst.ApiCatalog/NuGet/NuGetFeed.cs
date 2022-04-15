using System.Net.Http.Json;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public sealed class NuGetFeed
{
    public NuGetFeed(string feedUrl)
    {
        FeedUrl = feedUrl;
    }

    public string FeedUrl { get; }

    public async Task<IReadOnlyList<NuGetVersion>> GetAllVersionsAsync(string packageId)
    {
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(FeedUrl);
        var resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);

        var versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: false, cache, logger,  cancellationToken);;
        return versions.ToArray();
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        var url = await GetPackageUrlAsync(identity);

        using var httpClient = new HttpClient();
        var nupkgStream = await httpClient.GetStreamAsync(url);
        return new PackageArchiveReader(nupkgStream);
    }

    public async Task CopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        var url = await GetPackageUrlAsync(identity);

        var retryCount = 3;
    Retry:
        try
        {
            using var httpClient = new HttpClient();
            var nupkgStream = await httpClient.GetStreamAsync(url);
            await nupkgStream.CopyToAsync(destination);
        }
        catch (Exception ex) when (retryCount > 0)
        {
            retryCount--;
            Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
            goto Retry;
        }
    }

    private async Task<string> GetPackageUrlAsync(PackageIdentity identity)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var packageBaseAddress = serviceIndex.GetServiceEntryUri("PackageBaseAddress/3.0.0")?.ToString();

        var id = identity.Id.ToLowerInvariant();
        var version = identity.Version.ToNormalizedString().ToLowerInvariant();
        return $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg";
    }

    public Task<Dictionary<string, string[]>> GetOwnerMappingAsync()
    {
        if (FeedUrl != NuGetFeeds.NuGetOrg)
            throw new NotSupportedException("We can only retrieve owner information for nuget.org");

        var httpClient = new HttpClient();
        var url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-014/owners/owners.v2.json";
        return httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url);
    }
}