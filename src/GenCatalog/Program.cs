using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Cataloging;
using Azure.Core;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration.UserSecrets;

using NuGet.Versioning;

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

        var rootPath = args.Length == 1
            ? args[0]
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

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
        var netfxCompatLabPath = Path.Combine(apiUsagesPath, "NetFx Compat Lab.tsv");
        var databasePath = Path.Combine(rootPath, "apicatalog.db");
        var compressedDatabasePath = Path.Combine(rootPath, "apicatalog.db.deflate");
        var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
        var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");

        var stopwatch = Stopwatch.StartNew();

        await DownloadArchivedPlatformsAsync(frameworksPath);
        await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
        await DownloadDotnetPackageListAsync(packageListPath);
        await DownloadNuGetUsages(nugetUsagesPath);
        await DownloadNetFxCompatLabUsages(netfxCompatLabPath);
        await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, frameworksPath);
        await GenerateCatalogDatabaseAsync(indexFrameworksPath, indexPackagesPath, apiUsagesPath, databasePath);
        await CompressCatalogDatabaseAsync(databasePath, compressedDatabasePath);
        await GenerateCatalogModel(databasePath, catalogModelPath);
        await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
        await UploadCatalogDatabaseAsync(compressedDatabasePath);
        await UploadCatalogModelAsync(catalogModelPath);
        await UploadSuffixTreeAsync(suffixTreePath);

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
            throw new Exception("Cannot retreive connection string for Azure blob storage. You either need to define an environment variable or a user secret.");

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
        await FrameworkDownloader.Download(archivePath, packsPath);
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
        var blobClient = new BlobClient(connectionString, "usage", "usages.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(nugetUsagesPath);
        File.SetLastAccessTimeUtc(nugetUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadNetFxCompatLabUsages(string netfxCompatLabPath)
    {
        if (File.Exists(netfxCompatLabPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(netfxCompatLabPath)!);

        Console.WriteLine("Downloading NetFx Compat Lab usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "netfxcompatlab.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(netfxCompatLabPath);
        File.SetLastAccessTimeUtc(netfxCompatLabPath, lastModified.UtcDateTime);
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

        var nugetFeed = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var nugetStore = new NuGetStore(nugetFeed, packagesPath);
        var packageIndexer = new PackageIndexer(nugetStore, frameworkLocators);

        var retryIndexed = false;
        var retryDisabled = false;
        var retryFailed = false;

        var document = XDocument.Load(packageListPath);
        Directory.CreateDirectory(packagesPath);

        var packages = document.Root!.Elements("package")
            .Select(e => (Id: e.Attribute("id")!.Value, Version: NuGetVersion.Parse(e.Attribute("version")!.Value)))
            .Where(t => PackageFilter.Default.IsMatch(t.Id))
            .GroupBy(t => t.Id)
            .Select(g => (Id: g.Key, Version: g.OrderBy(t => t.Version).Select(t => t.Version).Last().ToString()))
            .ToArray();

        foreach (var (id, version) in packages.OrderBy(t => t.Id))
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
                    if (packageEntry == null)
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
                    Console.WriteLine($"Failed: " + ex.Message);
                    File.Delete(disabledPath);
                    File.Delete(path);
                    File.WriteAllText(failedVersionPath, ex.ToString());
                }
            }
        }
    }

    private static Task GenerateCatalogDatabaseAsync(string platformsPath, string packagesPath, string usagesPath, string outputPath)
    {
        if (File.Exists(outputPath))
            return Task.CompletedTask;

        File.Delete(outputPath);

        using var builder = CatalogBuilder.Create(outputPath);
        builder.Index(platformsPath);
        builder.Index(packagesPath);

        var usageFiles = GetUsageFiles(usagesPath);
        foreach (var (path, name, date) in usageFiles)
            builder.IndexUsages(path, name, date);

        return Task.CompletedTask;
    }

    private static async Task CompressCatalogDatabaseAsync(string databasePath, string compressedDatabasePath)
    {
        await using var inputStream = File.OpenRead(databasePath);
        await using var outputStream = File.Create(compressedDatabasePath);
        await using var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
        await inputStream.CopyToAsync(deflateStream);
    }

    private static async Task GenerateCatalogModel(string databasePath, string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
            return;

        Console.WriteLine($"Generating {Path.GetFileName(catalogModelPath)}...");
        await ApiCatalogModel.ConvertAsync(databasePath, catalogModelPath);

        var model = ApiCatalogModel.Load(catalogModelPath);
        var stats = model.GetStatistics().ToString();
        Console.WriteLine("Catalog stats:");
        Console.Write(stats);
        await File.WriteAllTextAsync(Path.ChangeExtension(catalogModelPath, ".txt"), stats);
    }

    private static Task GenerateSuffixTreeAsync(string catalogModelPath, string suffixTreePath)
    {
        if (File.Exists(suffixTreePath))
            return Task.CompletedTask;

        Console.WriteLine($"Generating {Path.GetFileName(suffixTreePath)}...");
        var catalog = ApiCatalogModel.Load(catalogModelPath);
        var builder = new SuffixTreeBuilder();

        foreach (var api in catalog.GetAllApis())
        {
            if (api.Kind.IsAccessor())
                continue;

            builder.Add(api.ToString(), api.Id);
        }

        using var stream = File.Create(suffixTreePath);
        builder.WriteSuffixTree(stream);

        return Task.CompletedTask;
    }

    private static async Task UploadCatalogDatabaseAsync(string compressedDatabasePath)
    {
        Console.WriteLine("Uploading catalog database...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "apicatalog.db.deflate", options: GetBlobOptions());
        await blobClient.UploadAsync(compressedDatabasePath, overwrite: true);
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
            Console.WriteLine("warning: cannot retreive secret for GenCatalog web hook.");
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
            var date = DateOnly.FromDateTime(File.GetLastWriteTime(file));
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