using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using NuGet.Versioning;

using PackageIndexing;

namespace PackageAnalyzerTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var indexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Indexing");
            var platformsPath = Path.Combine(indexPath, "platforms");
            var packageListPath = Path.Combine(indexPath, "packages.xml");
            var packagesPath = Path.Combine(indexPath, "packages");

            var stopwatch = Stopwatch.StartNew();

            //await GeneratePlatformIndex(platformsPath);
            //await GeneratePackageIndex(packageListPath, packagesPath);
            await ProduceCatalogBinary(platformsPath, packagesPath, Path.Combine(indexPath, "apicatalog.dat"));
            await ProduceCatalogSQLite(platformsPath, packagesPath, Path.Combine(indexPath, "apicatalog.db"));

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        }

        private static async Task GeneratePlatformIndex(string platformsPath)
        {
            var frameworkResolvers = new FrameworkResolver[]
            {
                // InstalledNetCoreResolver.Instance,
                // InstalledNetFrameworkResolver.Instance
                new ArchivedFrameworkResolver(@"C:\Users\immo\Downloads\PlatformArchive")
            };

            var frameworks = frameworkResolvers.SelectMany(r => r.Resolve());

            Directory.CreateDirectory(platformsPath);

            foreach (var framework in frameworks)
            {
                var path = Path.Join(platformsPath, $"{framework.FrameworkName}.xml");
                Console.WriteLine($"Indexing {framework.FrameworkName}...");
                var frameworkEntry = await FrameworkIndexer.Index(framework.FrameworkName, framework.FileSet);
                using (var stream = File.Create(path))
                    frameworkEntry.Write(stream);
            }
        }

        private static async Task GeneratePackageIndex(string packageListPath, string packagesPath)
        {
            var existingPackagesOnly = false;

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

            (string Id, string Version)[] packages;

            if (existingPackagesOnly)
            {
                packages = Directory.GetFiles(packagesPath, "*.xml")
                                    .Select(ParsePackage)
                                    .ToArray();
            }
            else
            {
                packages = document.Root.Elements("package")
                                        .Select(e => (Id: e.Attribute("id").Value, Version: NuGetVersion.Parse(e.Attribute("version").Value)))
                                        .GroupBy(t => t.Id)
                                        .Select(g => (Id: g.Key, Version: g.OrderBy(t => t.Version).Select(t => t.Version).Last().ToString()))
                                        .ToArray();
            }

            foreach (var (id, version) in packages.OrderBy(t => t.Id))
            {
                var path = Path.Join(packagesPath, $"{id}-{version}.xml");
                var disabledPath = Path.Join(packagesPath, $"{id}-all.disabled");
                var disabledVersionPath = Path.Join(packagesPath, $"{id}-{version}.disabled");
                var failedVersionPath = Path.Join(packagesPath, $"{id}-{version}.failed");

                var alreadyIndexed = !existingPackagesOnly &&
                                     (
                                         File.Exists(path) ||
                                         File.Exists(disabledPath) ||
                                         File.Exists(disabledVersionPath) ||
                                         File.Exists(failedVersionPath)
                                     );

                File.Delete(disabledVersionPath);

                if (alreadyIndexed)
                {
                    Console.WriteLine($"Package {id} {version} already indexed.");
                }
                else
                {
                    Console.WriteLine($"Indexing {id} {version}...");
                    try
                    {
                        var packageEntry = await PackageIndexer.Index(id, version);
                        if (packageEntry == null)
                        {
                            Console.WriteLine($"Not a library package.");
                            File.WriteAllText(disabledPath, string.Empty);
                        }
                        else
                        {
                            using (var stream = File.Create(path))
                                packageEntry.Write(stream);
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
