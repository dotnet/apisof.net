using System.Collections.Concurrent;
using System.Diagnostics;
using GenUsageNuGet.Infra;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Features;
using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Storage;
using NuGetFeed = GenUsageNuGet.Infra.NuGetFeed;

namespace GenUsageNuGet;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            return await MainAsync(args);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task<int> MainAsync(string[] args)
    {
        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);

        if (args.Length == 0 ||
            args.Length == 1 && (args[0] == "-?" || args[0] == "-h" || args[0] == "--help"))
        {
            Console.Error.WriteLine($"usage: {exeName} <command> [OPTIONS+]");
            Console.Error.WriteLine($"Commands:");
            Console.Error.WriteLine($"  crawl                    Starts the crawling");
            Console.Error.WriteLine($"  crawl-package            Crawls a single package");
            return 0;
        }

        var command = args[0];
        args = args.Skip(1).ToArray();

        switch (command.ToLowerInvariant())
        {
            case "crawl":
                return await CrawlMainAsync(args);
            case "crawl-package":
                return await CrawlPackageMainAsync(args);
            default:
                Console.Error.WriteLine($"error: invalid command '{command}'");
                return 1;
        }
    }

    private static async Task<int> CrawlMainAsync(string[] args)
    {
        // Parse arguments

        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        var packageListPath = "";
        var outputPath = "";
        var help = false;

        var options = new OptionSet
        {
            $"usage: {exeName} [OPTIONS]+",
            { "p|package-list=", "{Path} to a file with package identities to crawl", v => packageListPath = v },
            { "o|output=", "{Path} to a directory where the package information will be stored", v => outputPath = v },
            { "h|?|help", null, _ => help = true, true }
        };

        try
        {
            var parameters = options.Parse(args).ToArray();

            if (help)
            {
                options.WriteOptionDescriptions(Console.Error);
                return 0;
            }

            // We don't take any parameters
            var unprocessed = parameters;

            if (unprocessed.Any())
            {
                foreach (var option in unprocessed)
                    Console.Error.WriteLine($"error: unrecognized argument {option}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var packageListCrawler = GetPackageListCrawler(packageListPath);
        var store = GetCrawlerStore(outputPath);
        await CrawlAsync(packageListCrawler, store);
        return 0;
    }

    private static async Task<int> CrawlPackageMainAsync(string[] args)
    {
        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        var help = false;

        string packageId;
        NuGetVersion packageVersion;
        string outputPath;

        var options = new OptionSet
        {
            $"usage: {exeName} crawl-package <package-id> <package-version> <output-path>",
            { "h|?|help", null, _ => help = true, true }
        };

        try
        {
            var parameters = options.Parse(args).ToArray();

            if (help)
            {
                options.WriteOptionDescriptions(Console.Error);
                return 0;
            }

            if (parameters.Length < 1)
            {
                Console.Error.WriteLine($"error: expected <package-id>");
                return 1;
            }

            if (parameters.Length < 2)
            {
                Console.Error.WriteLine($"error: expected <package-version>");
                return 1;
            }

            if (parameters.Length < 3)
            {
                Console.Error.WriteLine($"error: expected <output-path>");
                return 1;
            }

            packageId = parameters[0];
            packageVersion = NuGetVersion.Parse(parameters[1]);
            outputPath = parameters[2];

            var unprocessed = parameters.Skip(3).ToArray();

            if (unprocessed.Any())
            {
                foreach (var option in unprocessed)
                    Console.Error.WriteLine($"error: unrecognized argument {option}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var packageIdentity = new PackageIdentity(packageId, packageVersion);
        await CrawlPackageAsync(packageIdentity, outputPath);
        return 0;
    }

    private static PackageListCrawler GetPackageListCrawler(string packageListPath)
    {
        if (string.IsNullOrEmpty(packageListPath))
            return new NuGetPackageListCrawler(NuGetFeed.NuGetOrg);

        if (!File.Exists(packageListPath))
        {
            Console.Error.WriteLine($"error: package list file '{packageListPath}' doesn't exist");
            Environment.Exit(1);
        }

        return new FilePackageListCrawler(packageListPath);
    }

    private static CrawlerStore GetCrawlerStore(string outputPath)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (!string.IsNullOrEmpty(outputPath))
            return new FileSystemCrawlerStore(outputPath);

        var connectionString = config["AzureStorageConnectionString"];
        if (connectionString is null)
        {
            Console.Error.WriteLine("error: can't find configuration for AzureStorageConnectionString");
            Environment.Exit(1);
        }

        return new BlobStorageCrawlerStore(connectionString);
    }

    private static string GetScratchFilePath(string fileName)
    {
        var path = Path.Join(Environment.CurrentDirectory, "scratch", fileName);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        return path;
    }

    private static async Task CrawlAsync(PackageListCrawler packageListCrawler, CrawlerStore crawlerStore)
    {
        var apiCatalogPath = GetScratchFilePath("apicatalog.dat");
        var databasePath = GetScratchFilePath("usage.db");
        var usagesPath = GetScratchFilePath("usages.tsv");

        Console.WriteLine("Downloading API catalog...");

        await crawlerStore.DownloadApiCatalogAsync(apiCatalogPath);

        Console.WriteLine("Loading API catalog...");

        var apiCatalog = await ApiCatalogModel.LoadAsync(apiCatalogPath);

        Console.WriteLine("Downloading previously indexed usages...");

        await crawlerStore.DownloadDatabaseAsync(databasePath);

        using var usageDatabase = await NuGetUsageDatabase.OpenOrCreateAsync(databasePath);

        Console.WriteLine("Discovering existing packages...");

        var packagesWithVersions = (await usageDatabase.GetReferenceUnitsAsync()).ToArray();

        Console.WriteLine("Discovering latest packages...");

        var stopwatch = Stopwatch.StartNew();
        var packages = await packageListCrawler.GetPackagesAsync();

        Console.WriteLine($"Finished package discovery. Took {stopwatch.Elapsed}");
        Console.WriteLine($"Found {packages.Count:N0} package(s) in total.");

        packages = CollapseToLatestStableAndLatestPreview(packages);

        Console.WriteLine($"Found {packages.Count:N0} package(s) after collapsing to latest stable & latest preview.");

        var indexedPackages = new HashSet<PackageIdentity>(packagesWithVersions.Select(p => p.ReferenceUnit));
        var currentPackages = new HashSet<PackageIdentity>(packages);

        var packagesToBeDeleted = indexedPackages.Where(p => !currentPackages.Contains(p)).ToArray();
        var packagesToBeIndexed = currentPackages.Where(p => !indexedPackages.Contains(p)).ToArray();
        var packagesToBeReIndexed = packagesWithVersions.Where(pv => !packagesToBeDeleted.Contains(pv.ReferenceUnit) && pv.CollectorVersion < UsageCollectorSet.CurrentVersion)
            .Select(pv => pv.ReferenceUnit)
            .ToArray();

        Console.WriteLine($"Found {indexedPackages.Count:N0} package(s) in the index.");
        Console.WriteLine($"Found {packagesToBeDeleted.Length:N0} package(s) to remove from the index.");
        Console.WriteLine($"Found {packagesToBeIndexed.Length:N0} package(s) to add to the index.");
        Console.WriteLine($"Found {packagesToBeReIndexed.Length:N0} package(s) to be re-indexed.");

        Console.WriteLine($"Deleting packages...");

        stopwatch.Restart();
        await usageDatabase.DeleteReferenceUnitsAsync(packagesToBeDeleted);

        Console.WriteLine($"Finished deleting packages. Took {stopwatch.Elapsed}");

        stopwatch.Restart();

        var numberOfWorkers = Environment.ProcessorCount;
        Console.WriteLine($"Crawling using {numberOfWorkers} workers.");

        var crawlingTimeout = TimeSpan.FromHours(5);
        using var crawlingCancellationTokenSource = new CancellationTokenSource(crawlingTimeout);
        var crawlingCancellationToken = crawlingCancellationTokenSource.Token;

        var packagesToBeProcessed = packagesToBeIndexed.Concat(packagesToBeReIndexed);

        var inputQueue = new ConcurrentQueue<PackageIdentity>(packagesToBeProcessed);

        var outputQueue = new BlockingCollection<PackageResults>();

        var workers = Enumerable.Range(0, numberOfWorkers)
                                .Select(i => Task.Run(() => CrawlWorker(i, inputQueue, outputQueue, crawlingCancellationToken)))
                                .ToArray();

        var outputWorker = Task.Run(() => OutputWorker(usageDatabase, outputQueue));

        await Task.WhenAll(workers);

        outputQueue.CompleteAdding();
        await outputWorker;

        if (crawlingCancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Crawling interrupted because timeout of {crawlingTimeout} was exceeded.");
            Console.WriteLine($"There are {inputQueue.Count:N0} items left to index.");
        }

        Console.WriteLine($"Finished crawling. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Vacuuming database...");

        stopwatch.Restart();
        await usageDatabase.VacuumAsync();

        Console.WriteLine($"Finished vacuuming database. Took {stopwatch.Elapsed}");

        await usageDatabase.CloseAsync();

        Console.WriteLine($"Uploading database...");

        stopwatch.Restart();
        await crawlerStore.UploadDatabaseAsync(databasePath);

        Console.WriteLine($"Finished uploading database. Took {stopwatch.Elapsed}");

        await usageDatabase.OpenAsync();

        Console.WriteLine($"Deleting irrelevant features...");

        stopwatch.Restart();
        await usageDatabase.DeleteIrrelevantFeaturesAsync(apiCatalog);

        Console.WriteLine($"Finished deleting irrelevant features. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Inserting parent features...");

        stopwatch.Restart();
        await usageDatabase.InsertParentsFeaturesAsync(apiCatalog);

        Console.WriteLine($"Finished inserting parent features. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Exporting usages...");

        stopwatch.Restart();
        await usageDatabase.ExportUsagesAsync(usagesPath);

        Console.WriteLine($"Finished exporting usages. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Uploading usages...");

        await crawlerStore.UploadResultsAsync(usagesPath);

        static async Task CrawlWorker(int workerId,
                                      ConcurrentQueue<PackageIdentity> inputQueue,
                                      BlockingCollection<PackageResults> outputQueue,
                                      CancellationToken crawlingCancellationToken)
        {
            try
            {
                var fileName = GetScratchFilePath($"worker_{workerId:000}.txt");
                File.Delete(fileName);

                while (inputQueue.TryDequeue(out var packageId))
                {
                    if (crawlingCancellationToken.IsCancellationRequested)
                        break;

                    var log = await RunPackageCrawlerAsync(packageId, fileName);
                    var collectionResults = File.Exists(fileName)
                        ? await CollectionSetResults.LoadAsync(fileName)
                        : CollectionSetResults.Empty;

                    var results = new PackageResults(packageId, log, collectionResults);
                    outputQueue.Add(results);

                    File.Delete(fileName);
                }

                Console.WriteLine($"Crawl Worker {workerId:000} has finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Fatal] Crawl Worker crashed: " + ex);
                Environment.Exit(1);
            }
        }

        static async Task OutputWorker(NuGetUsageDatabase database,
                                       BlockingCollection<PackageResults> queue)
        {
            try
            {
                foreach (var (packageIdentity, logLines, collectionSetResults) in queue.GetConsumingEnumerable())
                {
                    foreach (var line in logLines)
                        Console.WriteLine($"[Crawler] {line}");

                    await database.DeleteReferenceUnitsAsync([packageIdentity]);
                    await database.AddReferenceUnitAsync(packageIdentity, UsageCollectorSet.CurrentVersion);

                    foreach (var featureSet in collectionSetResults.FeatureSets)
                    foreach (var feature in featureSet.Features)
                    {
                        await database.TryAddFeatureAsync(feature, featureSet.Version);
                        await database.AddUsageAsync(packageIdentity, feature);
                    }
                }

                Console.WriteLine("Output Worker has finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Fatal] Output Worker crashed: " + ex);
                Environment.Exit(1);
            }
        }
    }

    private static IReadOnlyList<PackageIdentity> CollapseToLatestStableAndLatestPreview(IEnumerable<PackageIdentity> packages)
    {
        var result = new List<PackageIdentity>();

        foreach (var pg in packages.GroupBy(p => p.Id))
        {
            var latestStable = pg.Where(p => !p.Version.IsPrerelease).MaxBy(p => p.Version);
            var latestPreview = pg.Where(p => p.Version.IsPrerelease).MaxBy(p => p.Version);

            if (latestStable is not null)
                result.Add(latestStable);

            if (latestPreview is not null)
            {
                if (latestStable is null || latestStable.Version < latestPreview.Version)
                    result.Add(latestPreview);
            }
        }

        return result.ToArray();
    }

    private static async Task<IReadOnlyList<string>> RunPackageCrawlerAsync(PackageIdentity packageId, string fileName)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            ArgumentList =
            {
                "crawl-package",
                packageId.Id,
                packageId.Version.ToString(),
                fileName
            },
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var processLog = new List<string>();
        var processLogLock = new object();

        process.ErrorDataReceived += OnDataReceived;
        process.OutputDataReceived += OnDataReceived;

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await process.WaitForExitAsync();

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
                return;

            lock (processLogLock)
                processLog.Add(e.Data);
        }

        processLog.Add($"Exit code = {process.ExitCode}");
        return processLog;
    }

    private static async Task CrawlPackageAsync(PackageIdentity packageId, string fileName)
    {
        Console.WriteLine($"Crawling {packageId}...");

        var results = await PackageCrawler.CrawlAsync(NuGetFeed.NuGetOrg, packageId);
        await results.SaveAsync(fileName);
    }

    private record PackageResults(PackageIdentity PackageIdentity,
                                  IReadOnlyCollection<string> Log,
                                  CollectionSetResults CollectionSetResults);
}
