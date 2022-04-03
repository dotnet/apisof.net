using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace ApiCatalog.NuGet;

public sealed class NuGetFeed
{
    public NuGetFeed(string feedUrl)
    {
        FeedUrl = feedUrl;
    }

    public string FeedUrl { get; }

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackages(DateTimeOffset? since = null)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl == null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        const int MaxDegreeOfParallelism = 64;

        ThreadPool.SetMinThreads(MaxDegreeOfParallelism, completionPortThreads: 4);
        ServicePointManager.DefaultConnectionLimit = MaxDegreeOfParallelism;
        ServicePointManager.MaxServicePointIdleTime = 10000;

        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12
        };
        using var httpClient = new HttpClient(handler);

        var indexString = await httpClient.GetStringAsync(catalogIndexUrl);
        var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString);

        // Find all pages in the catalog index.
        var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
        var catalogLeaves = new ConcurrentBag<CatalogLeaf>();

        var fetchLeafsTasks = RunInParallel(async () =>
        {
            while (pageItems.TryTake(out var pageItem))
            {
                if (since != null && pageItem.CommitTimeStamp < since.Value)
                    continue;

                var retryCount = 3;
            Retry:
                try
                {
                    // Download the catalog page and deserialize it.
                    var pageString = await httpClient.GetStringAsync(pageItem.Url);
                    var page = JsonConvert.DeserializeObject<CatalogPage>(pageString);

                    var pageLeafItems = page.Items;

                    foreach (var pageLeafItem in page.Items)
                    {
                        if (pageLeafItem.Type == "nuget:PackageDetails")
                            catalogLeaves.Add(pageLeafItem);
                    }
                }
                catch (Exception ex) when (retryCount > 0)
                {
                    retryCount--;
                    Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
                    goto Retry;
                }
            }
        });

        await Task.WhenAll(fetchLeafsTasks);

        return catalogLeaves
            .Select(l => new PackageIdentity(l.Id, NuGetVersion.Parse(l.Version)))
            .Distinct()
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version)
            .ToArray();

        static List<Task> RunInParallel(Func<Task> work)
        {
            return Enumerable.Range(0, MaxDegreeOfParallelism)
                .Select(i => work())
                .ToList();
        }
    }

    public async Task<IReadOnlyList<NuGetVersion>> GetAllVersionsAsync(string packageId)
    {
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(FeedUrl);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        var versions = await resource.GetAllVersionsAsync(packageId, cache, logger, cancellationToken);
        return versions.ToArray();
    }

    public Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
        return GetPackageAsync(identity);
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
            throw new NotSupportedException("We can only retreive owner information for nuget.org");

        var httpClient = new HttpClient();
        var url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-014/owners/owners.v2.json";
        return httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url);
    }

    private abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        [JsonProperty("commitTimeStamp")]
        public DateTime CommitTimeStamp { get; set; }
    }

    private sealed class CatalogIndex : CatalogEntity
    {
        public List<CatalogPage> Items { get; set; }
    }

    private sealed class CatalogPage : CatalogEntity
    {
        public List<CatalogLeaf> Items { get; set; }
    }

    private sealed class CatalogLeaf : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public string Id { get; set; }

        [JsonProperty("nuget:version")]
        public string Version { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }
    }
}