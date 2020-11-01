using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

using ApiCatalog;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration.UserSecrets;

using NuGet.Versioning;

namespace GenCatalog
{
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
            var databasePath = Path.Combine(rootPath, "apicatalog.db");

            var stopwatch = Stopwatch.StartNew();

            await DownloadArchivedPlatformsAsync(frameworksPath);
            await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
            await DownloadDotnetPackageListAsync(packageListPath);
            await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
            await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, indexFrameworksPath);
            await ProduceCatalogSQLiteAsync(indexFrameworksPath, indexPackagesPath, databasePath);
            await UploadCatalogAsync(databasePath);

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
        }

        private static string GetAzureStorageConnectionString()
        {
            var result = Environment.GetEnvironmentVariable("API_CATALOG_AZURE_STORAGE_CONNECTION_STRING");
            if (string.IsNullOrEmpty(result))
            {
                var secrets = Secrets.Load();
                result = secrets?.AzureStorageConnectionString;
            }

            if (string.IsNullOrEmpty(result))
                throw new Exception("Cannot retreive connection string for Azure blob storage. You either need to define an environment variable or a user secret.");

            return result;
        }

        private static string? GetAzureDevOpsUrl()
        {
            var baseUrl = Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONSERVERURI");
            var project = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT");
            var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID");

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(buildId))
                return null;

            return $"{baseUrl}/{project}/_build/results?buildId={buildId}&view=logs";
        }

        private static async Task DownloadArchivedPlatformsAsync(string archivePath)
        {
            var connectionString = GetAzureStorageConnectionString();
            var container = "archive";
            var containerClient = new BlobContainerClient(connectionString, container);

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                var nameWithoutExtension = Path.ChangeExtension(blob.Name, null);
                var localDirectory = Path.Combine(archivePath, nameWithoutExtension);
                if (!Directory.Exists(localDirectory))
                {
                    Console.WriteLine($"Downloading {nameWithoutExtension}...");
                    var blobClient = new BlobClient(connectionString, container, blob.Name);
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

        private static Task DownloadDotnetPackageListAsync(string packageListPath)
        {
            return DotnetPackageIndex.CreateAsync(packageListPath);
        }

        private static async Task GeneratePlatformIndexAsync(string frameworksPath, string indexFrameworksPath)
        {
            var frameworkResolvers = new FrameworkProvider[]
            {
                new ArchivedFrameworkProvider(frameworksPath),
                new PackBasedFrameworkProvider(frameworksPath)
            };

            var frameworks = frameworkResolvers.SelectMany(r => r.Resolve());
            var reindex = false;

            Directory.CreateDirectory(indexFrameworksPath);

            foreach (var framework in frameworks)
            {
                var path = Path.Join(indexFrameworksPath, $"{framework.FrameworkName}.xml");
                var alreadyIndexed = !reindex && File.Exists(path);

                if (alreadyIndexed)
                {
                    Console.WriteLine($"{framework.FrameworkName} already indexed.");
                }
                else
                {
                    Console.WriteLine($"Indexing {framework.FrameworkName}...");
                    var frameworkEntry = await FrameworkIndexer.Index(framework.FrameworkName, framework.FileSet);
                    using (var stream = File.Create(path))
                        frameworkEntry.Write(stream);
                }
            }
        }

        private static async Task GeneratePackageIndexAsync(string packageListPath, string packagesPath, string indexPackagesPath, string indexFrameworksPath)
        {
            var frameworkLocators = new FrameworkLocator[]
            {
                new ArchivedFrameworkLocator(indexFrameworksPath),
                new PackBasedFrameworkLocator(indexFrameworksPath),
                new PclFrameworkLocator(indexFrameworksPath)
            };

            Directory.CreateDirectory(packagesPath);
            Directory.CreateDirectory(indexPackagesPath);

            var nugetFeed = new NuGetFeed(NuGetFeeds.NuGetOrg);
            var nugetStore = new NuGetStore(nugetFeed, packagesPath);
            var packageIndexer = new PackageIndexer(nugetStore, frameworkLocators);

            var retryIndexed = true;
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
                        File.WriteAllText(failedVersionPath, ex.ToString());
                    }
                }
            }
        }

        private static async Task ProduceCatalogSQLiteAsync(string platformsPath, string packagesPath, string outputPath)
        {
            File.Delete(outputPath);

            var builder = await CatalogBuilderSQLite.CreateAsync(outputPath);
            builder.Index(platformsPath);
            builder.Index(packagesPath);
        }

        private static async Task UploadCatalogAsync(string databasePath)
        {
            var compressedFileName = databasePath + ".deflate";
            using (var inputStream = File.OpenRead(databasePath))
            using (var outputStream = File.Create(compressedFileName))
            using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
                await inputStream.CopyToAsync(deflateStream);

            Console.WriteLine("Uploading database...");
            var connectionString = GetAzureStorageConnectionString();
            var container = "catalog";
            var blobClient = new BlobClient(connectionString, container, "apicatalog.db.deflate");
            await blobClient.UploadAsync(compressedFileName, overwrite: true);
        }

        private static async Task UploadSummaryAsync(bool success)
        {
            var job = new Job
            {
                Date = DateTimeOffset.UtcNow,
                Success = success,
                DetailsUrl = GetAzureDevOpsUrl()
            };

            using var jobStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(jobStream, job);
            jobStream.Position = 0;

            var connectionString = GetAzureStorageConnectionString();
            var container = "catalog";
            var blobClient = new BlobClient(connectionString, container, "job.json");
            await blobClient.UploadAsync(jobStream, overwrite: true);
        }
    }

    internal sealed class Secrets
    {
        public string? AzureStorageConnectionString { get; set; }

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
}
