using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using PackageIndexing;

namespace PackageAnalyzerTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var indexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Indexing");

            var stopwatch = Stopwatch.StartNew();

            //await GenerateIndex(indexPath);
            await ProduceCatalogBinary(indexPath, Path.Combine(indexPath, "apicatalog.dat"));
            await ProduceCatalogSQLite(indexPath, Path.Combine(indexPath, "apicatalog.db"));

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        }

        private static async Task GenerateIndex(string indexPath)
        {
            var frameworkResolvers = new FrameworkResolver[]
            {
                // InstalledNetCoreResolver.Instance,
                // InstalledNetFrameworkResolver.Instance
                new ArchivedFrameworkResolver(@"C:\Users\immo\Downloads\PlatformArchive")
            };

            var frameworks = frameworkResolvers.SelectMany(r => r.Resolve());

            var packages = new[]
            {
                ("System.Memory", "4.5.4"),
                ("System.Collections.Immutable", "1.7.1"),
            };

            Directory.CreateDirectory(indexPath);

            foreach (var framework in frameworks)
            {
                var path = Path.Join(indexPath, $"{framework.FrameworkName}.xml");
                Console.WriteLine($"Indexing {framework.FrameworkName}...");
                var frameworkEntry = await FrameworkIndexer.Index(framework.FrameworkName, framework.FileSet);
                using (var stream = File.Create(path))
                    frameworkEntry.Write(stream);
            }

            foreach (var (id, version) in packages)
            {
                var path = Path.Join(indexPath, $"{id}-{version}.xml");
                Console.WriteLine($"Indexing {id} {version}...");
                var packageEntry = await PackageIndexer.Index(id, version);
                using (var stream = File.Create(path))
                    packageEntry.Write(stream);
            }
        }

        private static async Task ProduceCatalogBinary(string indexPath, string outputPath)
        {
            var builder = new CatalogBuilderBinary();
            builder.Index(indexPath);

            using (var stream = File.Create(outputPath))
                builder.WriteTo(stream);
        }

        private static async Task ProduceCatalogSQLite(string indexPath, string outputPath)
        {
            File.Delete(outputPath);

            var builder = await CatalogBuilderSQLite.CreateAsync(outputPath);
            builder.Index(indexPath);
        }
    }
}
