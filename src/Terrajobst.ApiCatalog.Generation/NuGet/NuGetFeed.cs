using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

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

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesAsync(DateTimeOffset? since = null)
    {
        if (TryGetAzureDevOpsFeed(FeedUrl, out var organization, out var project, out var feed))
            return await GetAllPackagesFromAzureDevOpsFeedAsync(organization, project, feed);

        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl == null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        var handler = new HttpClientHandler();
        handler.SslProtocols = SslProtocols.Tls12;

        using var httpClient = new HttpClient(handler);

        var indexString = await httpClient.GetStringAsync(catalogIndexUrl);
        var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString)!;

        // Find all pages in the catalog index.
        var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
        var catalogLeaves = new ConcurrentBag<CatalogLeaf>();

        var fetchLeavesTasks = RunInParallel(async () =>
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
                    var page = JsonConvert.DeserializeObject<CatalogPage>(pageString)!;

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

        await Task.WhenAll(fetchLeavesTasks);

        return catalogLeaves
            .Select(l => new PackageIdentity(l.Id, NuGetVersion.Parse(l.Version)))
            .Distinct()
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version)
            .ToArray();

        static List<Task> RunInParallel(Func<Task> work)
        {
            var maxDegreeOfParallelism = Environment.ProcessorCount * 2;
            return Enumerable.Range(0, maxDegreeOfParallelism)
                .Select(i => work())
                .ToList();
        }
    }

    private static async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesFromAzureDevOpsFeedAsync(string organization, string project, string feed)
    {
        var result = new List<PackageIdentity>();

        var client = new HttpClient();

        var skip = 0;

        while (true)
        {
            var url = new Uri($"https://feeds.dev.azure.com/{organization}/{project}/_apis/packaging/Feeds/{feed}/packages?api-version=7.1&$skip={skip}", UriKind.Absolute);
            var data = await client.GetStreamAsync(url);
            var document = JsonNode.Parse(data)!;

            var count = document["count"]!.GetValue<int>();
            if (count == 0)
                break;

            foreach (var element in document["value"]!.AsArray())
            {
                var name = element!["name"]!.GetValue<string>();

                foreach (var versionElement in element["versions"]!.AsArray())
                {
                    var versionText = versionElement!["version"]!.GetValue<string>();
                    var version = NuGetVersion.Parse(versionText);
                    var identity = new PackageIdentity(name, version);
                    result.Add(identity);
                }
            }

            skip += count;
        }

        return result;
    }

    public async Task<IReadOnlyList<NuGetVersion>> GetAllVersionsAsync(string packageId, bool includeUnlisted = false)
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var repository = Repository.Factory.GetCoreV3(FeedUrl);
        var resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);
        var versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: includeUnlisted, cache, logger, cancellationToken);

        return versions.ToArray();
    }

    public async Task<PackageIdentity?> ResolvePackageAsync(string packageId, VersionRange range)
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var repository = Repository.Factory.GetCoreV3(FeedUrl);
        var resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);
        var versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: true, cache, logger, cancellationToken);
        var bestMatch = versions.FindBestMatch(range, x => x);

        if (bestMatch is null)
            return null;

        return new PackageIdentity(packageId, bestMatch);
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        var url = await GetPackageUrlAsync(identity);

        using var httpClient = new HttpClient();
        var nupkgStream = await httpClient.GetStreamAsync(url);
        return new PackageArchiveReader(nupkgStream);
    }

    public async Task<bool> TryCopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        var url = await GetPackageUrlAsync(identity);

        var retryCount = 3;
    Retry:
        try
        {
            using var httpClient = new HttpClient();
            var nupkgStream = await httpClient.GetStreamAsync(url);
            await nupkgStream.CopyToAsync(destination);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex) when (retryCount > 0)
        {
            retryCount--;
            Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
            goto Retry;
        }
    }

    public async Task CopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        await TryCopyPackageStreamAsync(identity, destination);
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
        var url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-017/owners/owners.v2.json";
        return httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url)!;
    }

    private static bool TryGetAzureDevOpsFeed(string url,
                                              [MaybeNullWhen(false)] out string organization,
                                              [MaybeNullWhen(false)] out string project,
                                              [MaybeNullWhen(false)] out string feed)
    {
        var match = Regex.Match(url, """
            https\://pkgs\.dev\.azure\.com/(?<Organization>[^/]+)/(?<Project>[^/]+)/_packaging/(?<Feed>[^/]+)/nuget/v3/index\.json
            """);

        if (match.Success)
        {
            organization = match.Groups["Organization"].Value;
            project = match.Groups["Project"].Value;
            feed = match.Groups["Feed"].Value;
            return true;
        }
        else
        {
            organization = default;
            project = default;
            feed = default;
            return false;
        }
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
