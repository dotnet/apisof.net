using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using NuGet.Versioning;

using ApiCatalog;

namespace GenIndex
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var indexPath = Path.Combine(rootPath, "Indexing");
            var archivePath = Path.Combine(rootPath, "PlatformArchive");
            var platformsPath = Path.Combine(indexPath, "platforms");
            var packageListPath = Path.Combine(indexPath, "packages.xml");
            var packagesPath = Path.Combine(indexPath, "packages");

            var stopwatch = Stopwatch.StartNew();

            //await UpdatePlatforms(archivePath);
            await GeneratePlatformIndex(platformsPath);
            await GeneratePackageIndex(packageListPath, packagesPath);
            await ProduceCatalogBinary(platformsPath, packagesPath, Path.Combine(indexPath, "apicatalog.dat"));
            await ProduceCatalogSQLite(platformsPath, packagesPath, Path.Combine(indexPath, "apicatalog.db"));

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
        }

        private static async Task UpdatePlatforms(string archivePath)
        {
            await FrameworkDownloader.Download(archivePath);
        }

        private static async Task GeneratePlatformIndex(string platformsPath)
        {
            var frameworkResolvers = new FrameworkProvider[]
            {
                // InstalledNetCoreResolver.Instance,
                // InstalledNetFrameworkResolver.Instance
                new ArchivedFrameworkProvider(@"C:\Users\immo\Downloads\PlatformArchive")
            };

            var frameworks = frameworkResolvers.SelectMany(r => r.Resolve());
            var reindex = true;

            Directory.CreateDirectory(platformsPath);

            foreach (var framework in frameworks)
            {
                var path = Path.Join(platformsPath, $"{framework.FrameworkName}.xml");
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

        private static async Task GeneratePackageIndex(string packageListPath, string packagesPath)
        {
            var packageCachePath = Path.Combine(packagesPath, "cache");
            Directory.CreateDirectory(packageCachePath);

            var nugetFeed = new NuGetFeed(WellKnownNuGetFeeds.NuGetOrg);
            var nugetStore = new NuGetStore(nugetFeed, packageCachePath);

            var retryIndexed = true;
            var retryDisabled = false;
            var retryFailed = false;

            static (string Id, string Version) ParsePackage(string path)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var dashIndex = name.IndexOf('-');
                var id = name.Substring(0, dashIndex);
                var version = name.Substring(dashIndex + 1);
                return (id, version);
            }

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
                var path = Path.Join(packagesPath, $"{id}-{version}.xml");
                var disabledPath = Path.Join(packagesPath, $"{id}-all.disabled");
                var failedVersionPath = Path.Join(packagesPath, $"{id}-{version}.failed");

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

        private static async Task ProduceCatalogBinary(string platformsPath, string packagesPath, string outputPath)
        {
            var builder = new CatalogBuilderBinary();
            builder.Index(platformsPath);
            builder.Index(packagesPath);

            using (var stream = File.Create(outputPath))
                builder.WriteTo(stream);
        }

        private static async Task ProduceCatalogSQLite(string platformsPath, string packagesPath, string outputPath)
        {
            File.Delete(outputPath);

            var builder = await CatalogBuilderSQLite.CreateAsync(outputPath);
            builder.Index(platformsPath);
            builder.Index(packagesPath);
        }
    }
}
