using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

using ApiCatalog;
using ApiCatalog.CatalogModel;

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
            var databasePath = Path.Combine(rootPath, "apicatalog.db");
            var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
            var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");

            var stopwatch = Stopwatch.StartNew();

            await DownloadArchivedPlatformsAsync(frameworksPath);
            await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
            await DownloadDotnetPackageListAsync(packageListPath);
            await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
            await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, frameworksPath);
            await ProduceCatalogSQLiteAsync(indexFrameworksPath, indexPackagesPath, databasePath);
            await GenerateCatalogModel(databasePath, catalogModelPath);
            await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
            await UploadCatalogDatabaseAsync(databasePath);
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
            return $"{serverUrl}/{repository}/runs/{runId}";
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

        private static async Task DownloadDotnetPackageListAsync(string packageListPath)
        {
            if (!File.Exists(packageListPath))
                await DotnetPackageIndex.CreateAsync(packageListPath);
        }

        private static Task GeneratePlatformIndexAsync(string frameworksPath, string indexFrameworksPath)
        {
            var frameworkResolvers = new FrameworkProvider[]
            {
                new ArchivedFrameworkProvider(frameworksPath),
                new PackBasedFrameworkProvider(frameworksPath)
            };

            var frameworks = frameworkResolvers.SelectMany(r => r.Resolve());
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
                        File.WriteAllText(failedVersionPath, ex.ToString());
                    }
                }
            }
        }

        private static async Task ProduceCatalogSQLiteAsync(string platformsPath, string packagesPath, string outputPath)
        {
            if (File.Exists(outputPath))
                return;

            File.Delete(outputPath);

            using var builder = await CatalogBuilderSQLite.CreateAsync(outputPath);
            builder.Index(platformsPath);
            builder.Index(packagesPath);
        }

        private static async Task GenerateCatalogModel(string databasePath, string catalogModelPath)
        {
            if (File.Exists(catalogModelPath))
                return;

            Console.WriteLine($"Generating {Path.GetFileName(catalogModelPath)}...");
            await ApiCatalogModel.ConvertAsync(databasePath, catalogModelPath);
        }

        private static Task GenerateSuffixTreeAsync(string catalogModelPath, string suffixTreePath)
        {
            if (File.Exists(suffixTreePath))
                return Task.CompletedTask;

            Console.WriteLine($"Generating {Path.GetFileName(suffixTreePath)}...");
            var catalog = ApiCatalogModel.Load(catalogModelPath);
            var builder = new SuffixTreeBuilder();

            foreach (var api in catalog.GetAllApis())
                builder.Add(api.ToString(), api.Id);

            using var stream = File.Create(suffixTreePath);
            builder.WriteSuffixTree(stream);

            return Task.CompletedTask;
        }

        private static async Task UploadCatalogDatabaseAsync(string databasePath)
        {
            var compressedFileName = databasePath + ".deflate";
            using (var inputStream = File.OpenRead(databasePath))
            using (var outputStream = File.Create(compressedFileName))
            using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
                await inputStream.CopyToAsync(deflateStream);

            Console.WriteLine("Uploading catalog database...");
            var connectionString = GetAzureStorageConnectionString();
            var container = "catalog";
            var blobClient = new BlobClient(connectionString, container, "apicatalog.db.deflate");
            await blobClient.UploadAsync(compressedFileName, overwrite: true);
        }

        private static async Task UploadCatalogModelAsync(string catalogModelPath)
        {
            Console.WriteLine("Uploading catalog mode...");
            var connectionString = GetAzureStorageConnectionString();
            var container = "catalog";
            var name = Path.GetFileName(catalogModelPath);
            var blobClient = new BlobClient(connectionString, container, name);
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
            var blobClient = new BlobClient(connectionString, container, "suffixtree.dat.deflate");
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
            var blobClient = new BlobClient(connectionString, container, "job.json");
            await blobClient.UploadAsync(jobStream, overwrite: true);
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
}
