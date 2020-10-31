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

            try
            {
                await Run(rootPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }

            return 0;
        }

        private static async Task Run(string rootPath)
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

            await DownloadArchivedPlatforms(frameworksPath);
            await DownloadPackagedPlatforms(frameworksPath, packsPath);
            await DownloadDotnetPackageList(packageListPath);
            await GeneratePlatformIndex(frameworksPath, indexFrameworksPath);
            await GeneratePackageIndex(packageListPath, packagesPath, indexPackagesPath);
            await ProduceCatalogSQLite(indexFrameworksPath, indexPackagesPath, databasePath);
            await UploadCatalog(databasePath);

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
        }

        private static string GetAzureStorageConnectionString()
        {
            var result = Environment.GetEnvironmentVariable("API_CATALOG_AZURE_STORAGE_CONNECTION_STRING");
            if (string.IsNullOrEmpty(result))
            {
                var secrets = Secrets.Load();
                result = secrets.AzureStorageConnectionString;
            }

            return result;
        }

        private static async Task DownloadArchivedPlatforms(string archivePath)
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

        private static async Task DownloadPackagedPlatforms(string archivePath, string packsPath)
        {
            await FrameworkDownloader.Download(archivePath, packsPath);
        }

        private static Task DownloadDotnetPackageList(string packageListPath)
        {
            return DotnetPackageIndex.CreateAsync(packageListPath);
        }

        private static async Task GeneratePlatformIndex(string frameworksPath, string indexFrameworksPath)
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

        private static async Task GeneratePackageIndex(string packageListPath, string packagesPath, string indexPackagesPath)
        {
            Directory.CreateDirectory(packagesPath);
            Directory.CreateDirectory(indexPackagesPath);

            var nugetFeed = new NuGetFeed(NuGetFeeds.NuGetOrg);
            var nugetStore = new NuGetStore(nugetFeed, packagesPath);

            var retryIndexed = true;
            var retryDisabled = false;
            var retryFailed = false;

            var document = XDocument.Load(packageListPath);
            Directory.CreateDirectory(packagesPath);

            var packages = document.Root.Elements("package")
                                   .Select(e => (Id: e.Attribute("id").Value, Version: NuGetVersion.Parse(e.Attribute("version").Value)))
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
                        var packageEntry = await PackageIndexer.Index(id, version, nugetStore);
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

        private static async Task ProduceCatalogSQLite(string platformsPath, string packagesPath, string outputPath)
        {
            File.Delete(outputPath);

            var builder = await CatalogBuilderSQLite.CreateAsync(outputPath);
            builder.Index(platformsPath);
            builder.Index(packagesPath);
        }

        private static async Task UploadCatalog(string databasePath)
        {
            var compressedFileName = databasePath + ".deflate";
            using var inpuStream = File.OpenRead(databasePath);
            using var outputStream = File.Create(compressedFileName);
            using var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
            await inpuStream.CopyToAsync(deflateStream);

            Console.WriteLine("Uploading database...");
            var connectionString = GetAzureStorageConnectionString();
            var container = "catalog";
            var blobClient = new BlobClient(connectionString, container, "apicatalog.db.deflate");
            await blobClient.UploadAsync(compressedFileName);
        }
    }

    internal sealed class Secrets
    {
        public string AzureStorageConnectionString { get; set; }

        public static Secrets Load()
        {
            var secretsPath = PathHelper.GetSecretsPathFromSecretsId("ApiCatalog");
            var secretsJson = File.ReadAllText(secretsPath);
            return JsonSerializer.Deserialize<Secrets>(secretsJson)!;
        }
    }
}
