using System.Collections.Concurrent;
using System.Net;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace GenUsageNuGet.Infra;

internal sealed class NuGetFeed
{
    private static readonly int s_maxDegreeOfParallelism = GetCatalogMaxDegreeOfParallelism();
    private static readonly TimeSpan s_httpTimeout = GetHttpTimeout();
    private static readonly HttpClient s_httpClient = CreateHttpClient();

    public static NuGetFeed NuGetOrg { get; } = new("https://api.nuget.org/v3/index.json");

    public NuGetFeed(string feedUrl)
    {
        ThrowIfNull(feedUrl);

        FeedUrl = feedUrl;
    }

    public string FeedUrl { get; }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = s_httpTimeout
        };
    }

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackages(DateTimeOffset? since = null)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl is null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        var maxDegreeOfParallelism = s_maxDegreeOfParallelism;

        ThreadPool.SetMinThreads(maxDegreeOfParallelism, completionPortThreads: 4);
        ServicePointManager.DefaultConnectionLimit = maxDegreeOfParallelism;
        ServicePointManager.MaxServicePointIdleTime = 10000;

        var indexString = await s_httpClient.GetStringAsync(catalogIndexUrl);
        var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString)!;

        // Find all pages in the catalog index.
        var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
        var packages = new ConcurrentDictionary<PackageIdentity, byte>();

        var fetchLeafsTasks = RunInParallel(async () =>
        {
            while (pageItems.TryTake(out var pageItem))
            {
                if (since is not null && pageItem.CommitTimeStamp < since.Value)
                    continue;

                var retryCount = 3;
            Retry:
                try
                {
                    // Download the catalog page and deserialize it.
                    var pageString = await s_httpClient.GetStringAsync(pageItem.Url);
                    var page = JsonConvert.DeserializeObject<CatalogPage>(pageString)!;

                    foreach (var pageLeafItem in page.Items)
                    {
                        if (pageLeafItem.Type == "nuget:PackageDetails")
                        {
                            var package = new PackageIdentity(pageLeafItem.Id, NuGetVersion.Parse(pageLeafItem.Version));
                            packages.TryAdd(package, 0);
                        }
                    }
                }
                catch (Exception ex) when (retryCount > 0)
                {
                    retryCount--;
                    var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, 3 - retryCount)));
                    Console.Error.WriteLine($"error: {ex.GetType().Name}: {ex.Message}, url = {pageItem.Url}, delay = {delay.TotalSeconds:N0}s, retries left = {retryCount}");
                    await Task.Delay(delay);
                    goto Retry;
                }
            }
        }, maxDegreeOfParallelism);

        await Task.WhenAll(fetchLeafsTasks);

        return packages.Keys
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version)
            .ToArray();

        static List<Task> RunInParallel(Func<Task> work, int degreeOfParallelism)
        {
            return Enumerable.Range(0, degreeOfParallelism)
                .Select(_ => work())
                .ToList();
        }
    }

    private static int GetCatalogMaxDegreeOfParallelism()
    {
        const int fallback = 4;
        const int min = 1;
        const int max = 16;

        var text = Environment.GetEnvironmentVariable("GENUSAGE_NUGET_CATALOG_DOP");
        return int.TryParse(text, out var value)
            ? Math.Clamp(value, min, max)
            : fallback;
    }

    private static TimeSpan GetHttpTimeout()
    {
        const int fallbackSeconds = 36000;
        const int minSeconds = 30;
        const int maxSeconds = 36000;

        var text = Environment.GetEnvironmentVariable("GENUSAGE_NUGET_HTTP_TIMEOUT_SECONDS");
        var seconds = int.TryParse(text, out var value)
            ? Math.Clamp(value, minSeconds, maxSeconds)
            : fallbackSeconds;

        return TimeSpan.FromSeconds(seconds);
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        ThrowIfNull(identity);

        var url = await GetPackageUrlAsync(identity);

        var nupkgStream = await s_httpClient.GetStreamAsync(url);
        return new PackageArchiveReader(nupkgStream);
    }

    private async Task<string> GetPackageUrlAsync(PackageIdentity identity)
    {
        ThrowIfNull(identity);

        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var packageBaseAddress = serviceIndex.GetServiceEntryUri("PackageBaseAddress/3.0.0")?.ToString();

        var id = identity.Id.ToLowerInvariant();
        var version = identity.Version.ToNormalizedString().ToLowerInvariant();
        return $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg";
    }

    private abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public required string Url { get; set; }

        [JsonProperty("commitTimeStamp")]
        public required DateTime CommitTimeStamp { get; set; }
    }

    private sealed class CatalogIndex : CatalogEntity
    {
        public required List<CatalogPage> Items { get; set; }
    }

    private sealed class CatalogPage : CatalogEntity
    {
        public required List<CatalogLeaf> Items { get; set; }
    }

    private sealed class CatalogLeaf : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public required string Id { get; set; }

        [JsonProperty("nuget:version")]
        public required string Version { get; set; }

        [JsonProperty("@type")]
        public required string Type { get; set; }
    }
}