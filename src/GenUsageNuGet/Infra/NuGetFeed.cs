using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NuGet.Configuration;
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
    private static readonly int s_packageDownloadMaxRetries = GetPackageDownloadMaxRetries();
    private static readonly HttpClient s_httpClient = CreateHttpClient();

    public static NuGetFeed NuGetOrg { get; } = new("https://devdiv.pkgs.visualstudio.com/OnlineServices/_packaging/dotnetlegacy/nuget/v3/index.json");
    private readonly Lazy<Task<ServiceIndexResourceV3>> _serviceIndex;
    private readonly Lazy<Task<string>> _packageBaseAddress;

    public NuGetFeed(string feedUrl)
    {
        ThrowIfNull(feedUrl);

        FeedUrl = feedUrl;

        _serviceIndex = new Lazy<Task<ServiceIndexResourceV3>>(() =>
        {
            var sourceRepository = GetSourceRepository();
            return sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        });

        _packageBaseAddress = new Lazy<Task<string>>(async () =>
        {
            var serviceIndex = await _serviceIndex.Value;
            var url = serviceIndex.GetServiceEntryUri("PackageBaseAddress/3.0.0")?.ToString();
            if (url is null)
                throw new InvalidOperationException("This feed doesn't expose PackageBaseAddress/3.0.0");
            return url;
        });
    }

    public string FeedUrl { get; }

    private static HttpClient CreateHttpClient()
    {
        // Use the default SocketsHttpHandler so the OS negotiates TLS (incl. TLS 1.3)
        return new HttpClient
        {
            Timeout = s_httpTimeout
        };
    }

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackages(DateTimeOffset? since = null)
    {
        if (TryGetAzureDevOpsFeed(FeedUrl, out var organization, out var project, out var feed))
            return await GetAllPackagesFromAzureDevOpsFeedAsync(organization, project, feed);

        var serviceIndex = await _serviceIndex.Value;
        var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl is null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        var maxDegreeOfParallelism = s_maxDegreeOfParallelism;

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

    private static async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesFromAzureDevOpsFeedAsync(string organization, string project, string feed)
    {
        var result = new List<PackageIdentity>();

        var feedUrl = $"https://{organization}.pkgs.visualstudio.com/{project}/_packaging/{feed}/nuget/v3/index.json";
        var hasCredentials = TryGetAzureArtifactsCredential(feedUrl, out var username, out var password);

        var skip = 0;

        while (true)
        {
            var url = new Uri($"https://feeds.dev.azure.com/{organization}/{project}/_apis/packaging/Feeds/{feed}/packages?api-version=7.1&$skip={skip}", UriKind.Absolute);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (hasCredentials)
            {
                var token = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
            }

            using var response = await s_httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var snippet = content.Length > 512
                    ? content[..512]
                    : content;

                throw new InvalidOperationException($"Azure DevOps package listing failed: {(int)response.StatusCode} {response.ReasonPhrase}. url={url}. body={snippet}");
            }

            JsonNode? document;

            try
            {
                document = JsonNode.Parse(content);
            }
            catch (System.Text.Json.JsonException ex)
            {
                var snippet = content.Length > 512
                    ? content[..512]
                    : content;

                throw new InvalidOperationException($"Azure DevOps package listing returned non-JSON content. url={url}. body={snippet}", ex);
            }

            if (document is null)
                throw new InvalidOperationException($"Azure DevOps package listing returned empty JSON content. url={url}");

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
        const int fallbackSeconds = 600;     // 10 minutes
        const int minSeconds = 30;
        const int maxSeconds = 36000;

        var text = Environment.GetEnvironmentVariable("GENUSAGE_NUGET_HTTP_TIMEOUT_SECONDS");
        var seconds = int.TryParse(text, out var value)
            ? Math.Clamp(value, minSeconds, maxSeconds)
            : fallbackSeconds;

        return TimeSpan.FromSeconds(seconds);
    }

    private static int GetPackageDownloadMaxRetries()
    {
        const int fallback = 5;
        const int min = 0;
        const int max = 10;

        var text = Environment.GetEnvironmentVariable("GENUSAGE_NUGET_DOWNLOAD_RETRIES");
        return int.TryParse(text, out var value)
            ? Math.Clamp(value, min, max)
            : fallback;
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        ThrowIfNull(identity);

        var url = await GetPackageUrlAsync(identity);
        var downloadUrl = new Uri(url, UriKind.Absolute);

        if (TryGetAzureArtifactsCredential(FeedUrl, out var username, out var password))
        {
            var token = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
            using var response = await SendWithRetriesAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
                return request;
            });

            response.EnsureSuccessStatusCode();

            // PackageArchiveReader requires the stream to remain valid for the reader lifetime,
            // so we materialize the payload into memory before disposing the response.
            var networkStream = await response.Content.ReadAsStreamAsync();
            var memoryStream = new MemoryStream();
            await networkStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new PackageArchiveReader(memoryStream);
        }

        using var responseWithoutCredentials = await SendWithRetriesAsync(() => new HttpRequestMessage(HttpMethod.Get, downloadUrl));
        responseWithoutCredentials.EnsureSuccessStatusCode();

        // PackageArchiveReader requires the stream to remain valid for the reader lifetime,
        // so we materialize the payload into memory before disposing the response.
        var networkStreamWithoutCredentials = await responseWithoutCredentials.Content.ReadAsStreamAsync();
        var memoryStreamWithoutCredentials = new MemoryStream();
        await networkStreamWithoutCredentials.CopyToAsync(memoryStreamWithoutCredentials);
        memoryStreamWithoutCredentials.Position = 0;

        return new PackageArchiveReader(memoryStreamWithoutCredentials);
    }

    private static async Task<HttpResponseMessage> SendWithRetriesAsync(Func<HttpRequestMessage> createRequest)
    {
        var retriesLeft = s_packageDownloadMaxRetries;

        while (true)
        {
            try
            {
                using var request = createRequest();
                var response = await s_httpClient.SendAsync(request);

                if (!IsTransientStatusCode(response.StatusCode) || retriesLeft == 0)
                    return response;

                var delay = GetRetryDelay(response, retriesLeft);
                Console.Error.WriteLine($"warning: transient HTTP {(int)response.StatusCode} {response.ReasonPhrase}; retrying package download in {delay.TotalSeconds:N0}s ({retriesLeft} retries left)");

                response.Dispose();
                await Task.Delay(delay);
                retriesLeft--;
            }
            catch (HttpRequestException ex) when (retriesLeft > 0)
            {
                var delay = GetRetryDelay(response: null, retriesLeft);
                Console.Error.WriteLine($"warning: transient request failure ({ex.Message}); retrying package download in {delay.TotalSeconds:N0}s ({retriesLeft} retries left)");
                await Task.Delay(delay);
                retriesLeft--;
            }
            catch (TaskCanceledException) when (retriesLeft > 0)
            {
                var delay = GetRetryDelay(response: null, retriesLeft);
                Console.Error.WriteLine($"warning: package download timed out; retrying in {delay.TotalSeconds:N0}s ({retriesLeft} retries left)");
                await Task.Delay(delay);
                retriesLeft--;
            }
        }
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.TooManyRequests ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage? response, int retriesLeft)
    {
        var retryAfterDelta = response?.Headers.RetryAfter?.Delta;
        if (retryAfterDelta is not null && retryAfterDelta.Value > TimeSpan.Zero)
            return retryAfterDelta.Value;

        var retryAfterDate = response?.Headers.RetryAfter?.Date;
        if (retryAfterDate is not null)
        {
            var serverDelay = retryAfterDate.Value - DateTimeOffset.UtcNow;
            if (serverDelay > TimeSpan.Zero)
                return serverDelay;
        }

        var attempt = s_packageDownloadMaxRetries - retriesLeft + 1;
        var delaySeconds = Math.Min(30, Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private async Task<string> GetPackageUrlAsync(PackageIdentity identity)
    {
        ThrowIfNull(identity);

        var packageBaseAddress = await _packageBaseAddress.Value;

        var id = identity.Id.ToLowerInvariant();
        var version = identity.Version.ToNormalizedString().ToLowerInvariant();
        return $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg";
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

            if (!string.Equals(endpointUrl.TrimEnd('/'), normalizedFeed, StringComparison.OrdinalIgnoreCase) &&
                !AreEquivalentAzureArtifactsFeeds(endpointUrl, feedUrl))
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

    private static bool AreEquivalentAzureArtifactsFeeds(string leftUrl, string rightUrl)
    {
        if (!TryGetAzureDevOpsFeed(leftUrl, out var leftOrganization, out var leftProject, out var leftFeed))
            return false;

        if (!TryGetAzureDevOpsFeed(rightUrl, out var rightOrganization, out var rightProject, out var rightFeed))
            return false;

        return string.Equals(leftOrganization, rightOrganization, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(leftProject, rightProject, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(leftFeed, rightFeed, StringComparison.OrdinalIgnoreCase);
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
        // New format: https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json
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

        // Legacy format: https://{org}.pkgs.visualstudio.com/{project}/_packaging/{feed}/nuget/v3/index.json
        var legacyMatch = Regex.Match(url, """
            https\://(?<Organization>[^.]+)\.pkgs\.visualstudio\.com/(?<Project>[^/]+)/_packaging/(?<Feed>[^/]+)/nuget/v3/index\.json
            """);

        if (legacyMatch.Success)
        {
            organization = legacyMatch.Groups["Organization"].Value;
            project = legacyMatch.Groups["Project"].Value;
            feed = legacyMatch.Groups["Feed"].Value;
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