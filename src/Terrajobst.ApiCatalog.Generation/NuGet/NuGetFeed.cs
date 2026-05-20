using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Model;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public sealed class NuGetFeed
{
    private static readonly HttpClient s_httpClient = CreateHttpClient();

    public NuGetFeed(string feedUrl)
    {
        FeedUrl = feedUrl;
    }

    public string FeedUrl { get; }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12
        };

        return new HttpClient(handler, disposeHandler: true);
    }

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesAsync(DateTimeOffset? since = null)
    {
        if (TryGetAzureDevOpsFeed(FeedUrl, out var organization, out var project, out var feed))
            return await GetAllPackagesFromAzureDevOpsFeedAsync(organization, project, feed);

        var sourceRepository = GetSourceRepository();
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl == null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        var indexString = await s_httpClient.GetStringAsync(catalogIndexUrl);
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
                    var pageString = await s_httpClient.GetStringAsync(pageItem.Url);
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

        var skip = 0;

        while (true)
        {
            var url = new Uri($"https://feeds.dev.azure.com/{organization}/{project}/_apis/packaging/Feeds/{feed}/packages?api-version=7.1&$skip={skip}", UriKind.Absolute);
            using var data = await s_httpClient.GetStreamAsync(url);
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

        var repository = GetSourceRepository();
        var resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);
        var versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: includeUnlisted, cache, logger, cancellationToken);

        return versions.ToArray();
    }

    public async Task<PackageDeprecationMetadata?> GetDeprecationMetadata(PackageIdentity identity)
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var repository = GetSourceRepository();
        var resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);
        if (resource is null)
            return null;

        var packageMetadata = await resource.GetMetadataAsync(identity, cache, logger, cancellationToken);
        if (packageMetadata is null)
            return null;

        var deprecationMetadata = await packageMetadata.GetDeprecationMetadataAsync();
        return deprecationMetadata;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, IReadOnlyList<PackageVulnerabilityInfo>>>> GetVulnerabilities()
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var repository = GetSourceRepository();
        var resource = await repository.GetResourceAsync<IVulnerabilityInfoResource>(cancellationToken);
        if (resource is null)
            return Array.Empty<IReadOnlyDictionary<string, IReadOnlyList<PackageVulnerabilityInfo>>>();

        var packageMetadata = await resource.GetVulnerabilityInfoAsync(cache, logger, cancellationToken);
        return packageMetadata.KnownVulnerabilities ?? Array.Empty<IReadOnlyDictionary<string, IReadOnlyList<PackageVulnerabilityInfo>>>();
    }

    public async Task<PackageIdentity?> ResolvePackageAsync(string packageId, VersionRange range)
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;

        var repository = GetSourceRepository();
        var resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);
        var versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: true, cache, logger, cancellationToken);
        var bestMatch = versions.FindBestMatch(range, x => x);

        if (bestMatch is null)
            return null;

        return new PackageIdentity(packageId, bestMatch);
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        var stream = new MemoryStream();
        var success = await TryCopyPackageStreamAsync(identity, stream);

        if (!success)
            throw new Exception($"Can't resolve package {identity.Id} {identity.Version}");

        stream.Position = 0;
        return new PackageArchiveReader(stream);
    }

    public async Task<bool> TryCopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        var cache = NullSourceCacheContext.Instance;
        var logger = NullLogger.Instance;
        var cancellationToken = CancellationToken.None;
        var repository = GetSourceRepository();
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var retryCount = 3;
    Retry:
        try
        {
            return await resource.CopyNupkgToStreamAsync(identity.Id,
                                                         identity.Version,
                                                         destination,
                                                         cache,
                                                         logger,
                                                         cancellationToken);
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

    public async Task<Dictionary<string, string[]>> GetOwnerMappingAsync()
    {
        if (FeedUrl != NuGetFeeds.NuGetOrg)
            throw new NotSupportedException("We can only retrieve owner information for nuget.org");

        var url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-017/owners/owners.v2.json";
        var mapping = await s_httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url);
        return mapping ?? new Dictionary<string, string[]>();
    }

    private SourceRepository GetSourceRepository()
    {
        var settings = LoadSettings();
        var packageSourceProvider = new PackageSourceProvider(settings);
        var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, Repository.Provider.GetCoreV3());

        PackageSource? packageSource = null;

        foreach (var repository in sourceRepositoryProvider.GetRepositories())
        {
            if (AreSameSource(repository.PackageSource.Source, FeedUrl))
            {
                packageSource = repository.PackageSource;
                break;
            }
        }

        packageSource ??= new PackageSource(FeedUrl);

        if (TryGetAzureArtifactsCredential(packageSource.Source, out var username, out var password))
        {
            packageSource.Credentials = new PackageSourceCredential(packageSource.Name ?? packageSource.Source,
                                                                    username,
                                                                    password,
                                                                    isPasswordClearText: true,
                                                                    validAuthenticationTypesText: null);
        }

        return new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
    }

    private static ISettings LoadSettings()
    {
        foreach (var candidate in GetSettingsRoots())
        {
            var configPath = Path.Combine(candidate, "nuget.config");
            if (File.Exists(configPath))
                return Settings.LoadSpecificSettings(candidate, "nuget.config");
        }

        return Settings.LoadDefaultSettings(root: null);
    }

    private static IEnumerable<string> GetSettingsRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateRoots(Environment.CurrentDirectory))
        {
            if (seen.Add(root))
                yield return root;

            var srcRoot = Path.Combine(root, "src");
            if (seen.Add(srcRoot))
                yield return srcRoot;
        }

        foreach (var root in EnumerateRoots(AppContext.BaseDirectory))
        {
            if (seen.Add(root))
                yield return root;

            var srcRoot = Path.Combine(root, "src");
            if (seen.Add(srcRoot))
                yield return srcRoot;
        }
    }

    private static IEnumerable<string> EnumerateRoots(string startPath)
    {
        var directory = new DirectoryInfo(startPath);

        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    // VSS_NUGET_EXTERNAL_FEED_ENDPOINTS format set by NuGetAuthenticate@1:
    // {"endpointCredentials":[{"endpoint":"https://...","username":"user","password":"token"},...]}
    private static bool TryGetCredentialFromPipelineEnv(string feedUrl,
                                                        [MaybeNullWhen(false)] out string username,
                                                        [MaybeNullWhen(false)] out string password)
    {
        username = default;
        password = default;

        var envValue = Environment.GetEnvironmentVariable("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS");
        if (string.IsNullOrWhiteSpace(envValue))
            return false;

        var document = JsonNode.Parse(envValue);
        var endpoints = document?["endpointCredentials"]?.AsArray();
        if (endpoints is null)
            return false;

        var normalizedFeed = feedUrl.TrimEnd('/');

        foreach (var endpoint in endpoints)
        {
            var endpointUrl = endpoint?["endpoint"]?.GetValue<string>();
            if (string.IsNullOrEmpty(endpointUrl))
                continue;

            if (!string.Equals(endpointUrl.TrimEnd('/'), normalizedFeed, StringComparison.OrdinalIgnoreCase))
                continue;

            var endpointUsername = endpoint?["username"]?.GetValue<string>();
            var endpointPassword = endpoint?["password"]?.GetValue<string>();

            if (!string.IsNullOrWhiteSpace(endpointPassword))
            {
                username = string.IsNullOrWhiteSpace(endpointUsername) ? "VssSessionToken" : endpointUsername;
                password = endpointPassword;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetAzureArtifactsCredential(string feedUrl,
                                                       [MaybeNullWhen(false)] out string username,
                                                       [MaybeNullWhen(false)] out string password)
    {
        // NuGetAuthenticate@1 in Azure Pipelines sets VSS_NUGET_EXTERNAL_FEED_ENDPOINTS with
        // pre-configured credentials. Check this first so pipeline runs work without the
        // credential provider binary being present on the agent.
        if (TryGetCredentialFromPipelineEnv(feedUrl, out username, out password))
            return true;

        username = default;
        password = default;

        foreach (var providerPath in GetCredentialProviderPaths())
        {
            if (!File.Exists(providerPath))
                continue;

            var startInfo = new ProcessStartInfo(providerPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-U");
            startInfo.ArgumentList.Add(feedUrl);
            startInfo.ArgumentList.Add("-N");
            startInfo.ArgumentList.Add("-I");
            startInfo.ArgumentList.Add("-V");
            startInfo.ArgumentList.Add("Minimal");
            startInfo.ArgumentList.Add("-F");
            startInfo.ArgumentList.Add("Json");

            using var process = Process.Start(startInfo);
            if (process is null)
                continue;

            var output = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                continue;

            var document = JsonNode.Parse(output);
            var providerUsername = document?["Username"]?.GetValue<string>();
            var providerPassword = document?["Password"]?.GetValue<string>();

            if (!string.IsNullOrWhiteSpace(providerUsername) && !string.IsNullOrWhiteSpace(providerPassword))
            {
                username = providerUsername;
                password = providerPassword;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCredentialProviderPaths()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        yield return Path.Combine(userProfile, ".nuget", "plugins", "netfx", "CredentialProvider.Microsoft", "CredentialProvider.Microsoft.exe");
        yield return Path.Combine(userProfile, ".nuget", "plugins", "netcore", "CredentialProvider.Microsoft", "CredentialProvider.Microsoft.exe");
    }

    private static bool AreSameSource(string left, string right)
    {
        if (Uri.TryCreate(left, UriKind.Absolute, out var leftUri) &&
            Uri.TryCreate(right, UriKind.Absolute, out var rightUri))
        {
            return string.Equals(leftUri.AbsoluteUri.TrimEnd('/'), rightUri.AbsoluteUri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
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
