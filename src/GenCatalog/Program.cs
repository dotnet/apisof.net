using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Azure.Core;
using Azure.Storage.Blobs;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration.UserSecrets;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Features;
using Terrajobst.ApiCatalog.Generation.DesignNotes;

namespace GenCatalog;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length > 1)
        {
            var exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("error: incorrect number of arguments");
            Console.Error.Write($"usage: {exeName} [<download-directory>]");
            return -1;
        }

        var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
        var defaultPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Catalog");
        var rootPath = args.Length == 1
            ? args[0]
            : environmentPath ?? defaultPath;

        var success = true;

        try
        {
            await RunAsync(rootPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        try
        {
            await UploadSummaryAsync(success);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        if (success)
            await PostToGenCatalogWebHook();

        return success ? 0 : -1;
    }

    private static async Task RunAsync(string rootPath)
    {
        var indexPath = Path.Combine(rootPath, "index");
        var indexFrameworksPath = Path.Combine(indexPath, "frameworks");
        var indexPackagesPath = Path.Combine(indexPath, "packages");
        var packagesPath = Path.Combine(rootPath, "packages");
        var packageListPath = Path.Combine(packagesPath, "packages.xml");
        var frameworksPath = Path.Combine(rootPath, "frameworks");
        var packsPath = Path.Combine(rootPath, "packs");
        var apiUsagesPath = Path.Combine(rootPath, "api-usages");
        var nugetUsagesPath = Path.Combine(apiUsagesPath, "nuget.org.tsv");
        var plannerUsagesPath = Path.Combine(apiUsagesPath, "Upgrade Planner.tsv");
        var netfxCompatLabPath = Path.Combine(apiUsagesPath, "NetFx Compat Lab.tsv");
        var reviewRepoPath = Path.Combine(rootPath, "apireviews");
        var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
        var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");
        var usageDataPath = Path.Combine(rootPath, "usageData.dat");
        var designNotesPath = Path.Combine(rootPath, "designNotes.dat");

        var stopwatch = Stopwatch.StartNew();

        await DownloadArchivedPlatformsAsync(frameworksPath);
        await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
        await DownloadDotnetPackageListAsync(packageListPath);
        await DownloadNuGetUsages(nugetUsagesPath);
        await DownloadPlannerUsages(plannerUsagesPath);
        await DownloadNetFxCompatLabUsages(netfxCompatLabPath);
        await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, frameworksPath);
        await GenerateCatalogAsync(indexFrameworksPath, indexPackagesPath, apiUsagesPath, catalogModelPath);
        await GenerateUsageDataAsync(usageDataPath, apiUsagesPath);
        await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
        await CloneApiReviewsAsync(reviewRepoPath);
        await GenerateDesignNotesAsync(reviewRepoPath, catalogModelPath, designNotesPath);
        await UploadCatalogModelAsync(catalogModelPath);
        await UploadUsageData(usageDataPath);
        await UploadSuffixTreeAsync(suffixTreePath);
        await UploadDesignNotesAsync(designNotesPath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private static string GetAzureStorageConnectionString()
    {
        var result = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
        if (string.IsNullOrEmpty(result))
        {
            var secrets = Secrets.Load();
            result = secrets?.AzureStorageConnectionString;
        }

        if (string.IsNullOrEmpty(result))
            throw new Exception("Cannot retrieve connection string for Azure blob storage. You either need to define an environment variable or a user secret.");

        return result;
    }

    private static string? GetDetailsUrl()
    {
        var serverUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");
        var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        return $"{serverUrl}/{repository}/actions/runs/{runId}";
    }

    private static async Task DownloadArchivedPlatformsAsync(string archivePath)
    {
        var connectionString = GetAzureStorageConnectionString();
        var container = "archive";
        var containerClient = new BlobContainerClient(connectionString, container, options: GetBlobOptions());

        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            var nameWithoutExtension = Path.ChangeExtension(blob.Name, null);
            var localDirectory = Path.Combine(archivePath, nameWithoutExtension);
            if (!Directory.Exists(localDirectory))
            {
                Console.WriteLine($"Downloading {nameWithoutExtension}...");
                var blobClient = new BlobClient(connectionString, container, blob.Name, options: GetBlobOptions());
                using var blobStream = await blobClient.OpenReadAsync();
                using var archive = new ZipArchive(blobStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(localDirectory);
            }
        }
    }

    private static async Task DownloadPackagedPlatformsAsync(string archivePath, string packsPath)
    {
        await FrameworkDownloader.DownloadAsync(archivePath, packsPath);
    }

    private static async Task DownloadDotnetPackageListAsync(string packageListPath)
    {
        if (!File.Exists(packageListPath))
            await DotnetPackageIndex.CreateAsync(packageListPath);
    }

    private static async Task DownloadNuGetUsages(string nugetUsagesPath)
    {
        if (File.Exists(nugetUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(nugetUsagesPath)!);

        Console.WriteLine("Downloading NuGet usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-nuget.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(nugetUsagesPath);
        File.SetLastWriteTimeUtc(nugetUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadPlannerUsages(string plannerUsagesPath)
    {
        if (File.Exists(plannerUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(plannerUsagesPath)!);

        Console.WriteLine("Downloading Planner usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-planner.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(plannerUsagesPath);
        File.SetLastWriteTimeUtc(plannerUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadNetFxCompatLabUsages(string netfxCompatLabPath)
    {
        if (File.Exists(netfxCompatLabPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(netfxCompatLabPath)!);

        Console.WriteLine("Downloading NetFx Compat Lab usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-netfxcompatlab.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(netfxCompatLabPath);
        File.SetLastWriteTimeUtc(netfxCompatLabPath, lastModified.UtcDateTime);
    }

    private static Task GeneratePlatformIndexAsync(string frameworksPath, string indexFrameworksPath)
    {
        var frameworkResolvers = new FrameworkProvider[]
        {
            new ArchivedFrameworkProvider(frameworksPath),
            new PackBasedFrameworkProvider(frameworksPath)
        };

        var frameworks = frameworkResolvers.SelectMany(r => r.Resolve())
            .OrderBy(t => t.FrameworkName);
        var reindex = false;

        Directory.CreateDirectory(indexFrameworksPath);

        foreach (var (frameworkName, paths) in frameworks)
        {
            var path = Path.Join(indexFrameworksPath, $"{frameworkName}.xml");
            var alreadyIndexed = !reindex && File.Exists(path);

            if (alreadyIndexed)
            {
                Console.WriteLine($"{frameworkName} already indexed.");
            }
            else
            {
                Console.WriteLine($"Indexing {frameworkName}...");
                var frameworkEntry = FrameworkIndexer.Index(frameworkName, paths);
                using (var stream = File.Create(path))
                    frameworkEntry.Write(stream);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task GeneratePackageIndexAsync(string packageListPath, string packagesPath, string indexPackagesPath, string frameworksPath)
    {
        var frameworkLocators = new FrameworkLocator[]
        {
            new ArchivedFrameworkLocator(frameworksPath),
            new PackBasedFrameworkLocator(frameworksPath),
            new PclFrameworkLocator(frameworksPath)
        };

        Directory.CreateDirectory(packagesPath);
        Directory.CreateDirectory(indexPackagesPath);

        var document = XDocument.Load(packageListPath);
        Directory.CreateDirectory(packagesPath);

        var packages = document.Root!.Elements("package")
            .Select(e => (
                Id: e.Attribute("id")!.Value,
                Version: e.Attribute("version")!.Value))
            .ToArray();

        var nightlies = new NuGetFeed(NuGetFeeds.NightlyLatest);
        var nuGetOrg = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var nugetStore = new NuGetStore(packagesPath, nightlies, nuGetOrg);
        var packageIndexer = new PackageIndexer(nugetStore, frameworkLocators);

        var retryIndexed = false;
        var retryDisabled = false;
        var retryFailed = false;

        foreach (var (id, version) in packages)
        {
            var path = Path.Join(indexPackagesPath, $"{id}-{version}.xml");
            var disabledPath = Path.Join(indexPackagesPath, $"{id}-all.disabled");
            var failedVersionPath = Path.Join(indexPackagesPath, $"{id}-{version}.failed");

            var alreadyIndexed = !retryIndexed && File.Exists(path) ||
                                 !retryDisabled && File.Exists(disabledPath) ||
                                 !retryFailed && File.Exists(failedVersionPath);

            if (alreadyIndexed)
            {
                if (File.Exists(path))
                    Console.WriteLine($"Package {id} {version} already indexed.");

                if (File.Exists(disabledPath))
                    nugetStore.DeleteFromCache(id, version);
            }
            else
            {
                Console.WriteLine($"Indexing {id} {version}...");
                try
                {
                    var packageEntry = await packageIndexer.Index(id, version);
                    if (packageEntry is null)
                    {
                        Console.WriteLine($"Not a library package.");
                        File.WriteAllText(disabledPath, string.Empty);
                        nugetStore.DeleteFromCache(id, version);
                    }
                    else
                    {
                        using (var stream = File.Create(path))
                            packageEntry.Write(stream);

                        File.Delete(disabledPath);
                        File.Delete(failedVersionPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: {ex}");
                    File.Delete(disabledPath);
                    File.Delete(path);
                    File.WriteAllText(failedVersionPath, ex.ToString());
                }
            }
        }
    }

    private static async Task GenerateCatalogAsync(string platformsPath, string packagesPath, string usagesPath, string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
            return;

        File.Delete(catalogModelPath);

        var builder = new CatalogBuilder();
        builder.Index(platformsPath);
        builder.Index(packagesPath);
        builder.Build(catalogModelPath);

        var model = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var stats = model.GetStatistics().ToString();
        Console.WriteLine("Catalog stats:");
        Console.Write(stats);
        await File.WriteAllTextAsync(Path.ChangeExtension(catalogModelPath, ".txt"), stats);
    }

    private static Task GenerateUsageDataAsync(string usageDataPath, string apiUsagesPath)
    {
        if (File.Exists(usageDataPath))
            return Task.CompletedTask;

        Console.WriteLine($"Generating {Path.GetFileName(usageDataPath)}...");

        var usageFiles = GetUsageFiles(apiUsagesPath);
        var data = new List<(FeatureUsageSource Source, IReadOnlyList<(Guid FeatureId, float Percentage)> Values)>();

        foreach (var (path, name, date) in usageFiles)
        {
            var usageSource = new FeatureUsageSource(name, date);
            var usageSourceData = ParseFile(path).ToArray();
            data.Add((usageSource, usageSourceData));
        }

        var usageData = new FeatureUsageData(data);
        usageData.Save(usageDataPath);

        return Task.CompletedTask;

        static IEnumerable<(Guid FeatureId, float Percentage)> ParseFile(string path)
        {
            using var streamReader = new StreamReader(path);

            while (streamReader.ReadLine() is { } line)
            {
                var tabIndex = line.IndexOf('\t');
                var lastTabIndex = line.LastIndexOf('\t');
                if (tabIndex > 0 && tabIndex == lastTabIndex)
                {
                    var guidTextSpan = line.AsSpan(0, tabIndex);
                    var percentageSpan = line.AsSpan(tabIndex + 1);

                    if (Guid.TryParse(guidTextSpan, out var featureId) &&
                        float.TryParse(percentageSpan, out var percentage))
                    {
                        yield return (featureId, percentage);
                    }
                }
            }
        }
    }

    private static async Task GenerateSuffixTreeAsync(string catalogModelPath, string suffixTreePath)
    {
        if (File.Exists(suffixTreePath))
            return;

        Console.WriteLine($"Generating {Path.GetFileName(suffixTreePath)}...");
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var builder = new SuffixTreeBuilder();

        foreach (var api in catalog.AllApis)
        {
            if (api.Kind.IsAccessor())
                continue;

            builder.Add(api.ToString(), api.Id);
        }

        await using var stream = File.Create(suffixTreePath);
        builder.WriteSuffixTree(stream);
    }

    private static Task CloneApiReviewsAsync(string reviewRepoPath)
    {
        if (Directory.Exists(reviewRepoPath))
            return Task.CompletedTask;

        var url = "https://github.com/dotnet/apireviews";
        Console.WriteLine($"Cloning {url}...");
        Repository.Clone(url, reviewRepoPath);
        return Task.CompletedTask;
    }

    private static async Task GenerateDesignNotesAsync(string reviewRepoPath, string catalogModelPath, string designNotesPath)
    {
        if (File.Exists(designNotesPath))
            return;

        Console.WriteLine("Generating design notes...");
        var reviewDatabase = ReviewDatabase.Load(reviewRepoPath);
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var linkDatabase = DesignNoteBuilder.Build(reviewDatabase, catalog);
        linkDatabase.Save(designNotesPath);
    }

    private static async Task UploadCatalogModelAsync(string catalogModelPath)
    {
        Console.WriteLine("Uploading catalog model...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var name = Path.GetFileName(catalogModelPath);
        var blobClient = new BlobClient(connectionString, container, name, options: GetBlobOptions());
        await blobClient.UploadAsync(catalogModelPath, overwrite: true);
    }

    private static async Task UploadUsageData(string usageDataPath)
    {
        Console.WriteLine("Uploading usage data...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "usage";
        var name = Path.GetFileName(usageDataPath);
        var blobClient = new BlobClient(connectionString, container, name, options: GetBlobOptions());
        await blobClient.UploadAsync(usageDataPath, overwrite: true);
    }

    private static async Task UploadSuffixTreeAsync(string suffixTreePath)
    {
        var compressedFileName = suffixTreePath + ".deflate";
        using (var inputStream = File.OpenRead(suffixTreePath))
        using (var outputStream = File.Create(compressedFileName))
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
            await inputStream.CopyToAsync(deflateStream);

        Console.WriteLine("Uploading suffix tree...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "suffixtree.dat.deflate", options: GetBlobOptions());
        await blobClient.UploadAsync(compressedFileName, overwrite: true);
    }

    private static async Task UploadDesignNotesAsync(string designNotesPath)
    {
        Console.WriteLine("Uploading design notes...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "designNotes.dat", options: GetBlobOptions());
        await blobClient.UploadAsync(designNotesPath, overwrite: true);
    }

    private static async Task PostToGenCatalogWebHook()
    {
        Console.WriteLine("Invoking webhook...");
        var secrets = Secrets.Load();

        var url = Environment.GetEnvironmentVariable("GenCatalogWebHookUrl");
        if (string.IsNullOrEmpty(url))
            url = secrets?.GenCatalogWebHookUrl;

        var secret = Environment.GetEnvironmentVariable("GenCatalogWebHookSecret");
        if (string.IsNullOrEmpty(secret))
            secret = secrets?.GenCatalogWebHookSecret;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret))
        {
            Console.WriteLine("warning: cannot retrieve secret for GenCatalog web hook.");
            return;
        }

        try
        {
            var client = new HttpClient();
            var response = await client.PostAsync(url, new StringContent(secret));
            Console.WriteLine($"Webhook returned: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"warning: there was a problem calling the web hook: {ex}");
        }
    }

    private static async Task UploadSummaryAsync(bool success)
    {
        var job = new Job
        {
            Date = DateTimeOffset.UtcNow,
            Success = success,
            DetailsUrl = GetDetailsUrl()
        };

        using var jobStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jobStream, job);
        jobStream.Position = 0;

        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "job.json", options: GetBlobOptions());
        await blobClient.UploadAsync(jobStream, overwrite: true);
    }

    private static BlobClientOptions GetBlobOptions()
    {
        return new BlobClientOptions
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(90),
                MaxRetries = 10,
                NetworkTimeout = TimeSpan.FromMinutes(5),
            }
        };
    }

    private static IReadOnlyList<UsageFile> GetUsageFiles(string usagePath)
    {
        var result = new List<UsageFile>();
        var files = Directory.GetFiles(usagePath, "*.tsv");

        foreach (var file in files.OrderBy(f => f))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var date = DateOnly.FromDateTime(File.GetLastWriteTimeUtc(file));
            var usageFile = new UsageFile(file, name, date);
            result.Add(usageFile);
        }

        return result.ToArray();
    }
}

internal sealed class Secrets
{
    public string? AzureStorageConnectionString { get; set; }
    public string? GenCatalogWebHookUrl { get; set; }
    public string? GenCatalogWebHookSecret { get; set; }

    public static Secrets? Load()
    {
        var secretsPath = PathHelper.GetSecretsPathFromSecretsId("ApiCatalog");
        if (!File.Exists(secretsPath))
            return null;

        var secretsJson = File.ReadAllText(secretsPath);
        return JsonSerializer.Deserialize<Secrets>(secretsJson)!;
    }
}

internal sealed class Job
{
    public DateTimeOffset Date { get; set; }
    public bool Success { get; set; }
    public string? DetailsUrl { get; set; }
}

internal record UsageFile(string Path, string Name, DateOnly Date);