using System.Collections.Concurrent;
using System.Diagnostics;

using GenUsageNuGet.Infra;
using GenUsagePlanner;

using NuGet.Packaging.Core;

using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.ActionsRunner;
using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Storage;

namespace GenUsageNuGet;

internal sealed class CrawlMain : ConsoleCommand
{
    private readonly ScratchFileProvider _scratchFileProvider;
    private readonly ApisOfDotNetStore _store;
    private readonly GitHubActionsSummaryTable _summaryTable;

    public CrawlMain(ScratchFileProvider scratchFileProvider,
                     ApisOfDotNetStore store,
                     GitHubActionsSummaryTable summaryTable)
    {
        ThrowIfNull(scratchFileProvider);
        ThrowIfNull(store);
        ThrowIfNull(summaryTable);

        _scratchFileProvider = scratchFileProvider;
        _store = store;
        _summaryTable = summaryTable;
    }

    public override string Name => "crawl";

    public override string Description => "Starts the crawling";

    public override async Task ExecuteAsync()
    {
        var apiCatalogPath = _scratchFileProvider.GetScratchFilePath("apicatalog.dat");
        var databasePath = _scratchFileProvider.GetScratchFilePath("usages-nuget.db");
        var usagesPath = _scratchFileProvider.GetScratchFilePath("usages-nuget.tsv");

        Console.WriteLine("Downloading API catalog...");

        await _store.DownloadApiCatalogAsync(apiCatalogPath);

        Console.WriteLine("Loading API catalog...");

        var apiCatalog = await ApiCatalogModel.LoadAsync(apiCatalogPath);

        Console.WriteLine("Downloading previously indexed usages...");

        await _store.DownloadNuGetUsageDatabaseAsync(databasePath);

        using var usageDatabase = await NuGetUsageDatabase.OpenOrCreateAsync(databasePath);

        Console.WriteLine("Discovering existing packages...");

        var packagesWithVersions = (await usageDatabase.GetReferenceUnitsAsync()).ToArray();

        Console.WriteLine("Discovering latest packages...");

        var stopwatch = Stopwatch.StartNew();
        var packages = await GetAllPackagesAsync();

        Console.WriteLine($"Finished package discovery. Took {stopwatch.Elapsed}");
        Console.WriteLine($"Found {packages.Count:N0} package(s) in total.");
        _summaryTable.AppendNumber("#Packages (All)", packages.Count);

        packages = CollapseToLatestStableAndLatestPreview(packages);

        Console.WriteLine($"Found {packages.Count:N0} package(s) after collapsing to latest stable & latest preview.");
        _summaryTable.AppendNumber("#Packages (Latest Stable & Preview)", packages.Count);

        var indexedPackages = new HashSet<PackageIdentity>(packagesWithVersions.Select(p => p.ReferenceUnit));
        var currentPackages = new HashSet<PackageIdentity>(packages);

        var packagesToBeDeleted = indexedPackages.Where(p => !currentPackages.Contains(p)).ToArray();
        var packagesToBeIndexed = currentPackages.Where(p => !indexedPackages.Contains(p)).ToArray();
        var packagesToBeReIndexed = packagesWithVersions.Where(pv => !packagesToBeDeleted.Contains(pv.ReferenceUnit) && pv.CollectorVersion < UsageCollectorSet.CurrentVersion)
            .Select(pv => pv.ReferenceUnit)
            .ToArray();
        var packagesToBeCrawled = packagesToBeIndexed.Concat(packagesToBeReIndexed).ToArray();

        Console.WriteLine($"Found {indexedPackages.Count:N0} package(s) in the index.");
        Console.WriteLine($"Found {packagesToBeDeleted.Length:N0} package(s) to remove from the index.");
        Console.WriteLine($"Found {packagesToBeIndexed.Length:N0} package(s) to add to the index.");
        Console.WriteLine($"Found {packagesToBeReIndexed.Length:N0} package(s) to be re-indexed.");
        Console.WriteLine($"Found {packagesToBeCrawled.Length:N0} package(s) to be crawled.");

        _summaryTable.AppendNumber("#Packages in index", indexedPackages.Count);
        _summaryTable.AppendNumber("#Packages to be removed", packagesToBeDeleted.Length);
        _summaryTable.AppendNumber("#Packages to be added", packagesToBeIndexed.Length);
        _summaryTable.AppendNumber("#Packages to be re-indexed", packagesToBeReIndexed.Length);
        _summaryTable.AppendNumber("#Packages to be crawled", packagesToBeCrawled.Length);

        Console.WriteLine("Deleting packages...");

        stopwatch.Restart();
        await usageDatabase.DeleteReferenceUnitsAsync(packagesToBeDeleted);

        Console.WriteLine($"Finished deleting packages. Took {stopwatch.Elapsed}");

        stopwatch.Restart();
        await CrawlPackagesAsync(usageDatabase, packagesToBeCrawled);

        Console.WriteLine($"Finished crawling. Took {stopwatch.Elapsed}");

        Console.WriteLine("Deleting features without usages...");

        stopwatch.Restart();
        var featuresWithoutUsages = await usageDatabase.DeleteFeaturesWithoutUsagesAsync();

        Console.WriteLine($"Finished deleting features without usages. Deleted {featuresWithoutUsages:N0} features. Took {stopwatch.Elapsed}");
        _summaryTable.AppendNumber("#Features without usages", featuresWithoutUsages);

        Console.WriteLine("Vacuuming database...");

        stopwatch.Restart();
        await usageDatabase.VacuumAsync();

        Console.WriteLine($"Finished vacuuming database. Took {stopwatch.Elapsed}");

        await usageDatabase.CloseAsync();

        Console.WriteLine("Uploading database...");

        stopwatch.Restart();
        await _store.UploadNuGetUsageDatabaseAsync(databasePath);

        Console.WriteLine($"Finished uploading database. Took {stopwatch.Elapsed}");

        var databaseSize = new FileInfo(databasePath).Length;
        await usageDatabase.OpenAsync();

        Console.WriteLine("Getting statistics...");

        stopwatch.Restart();
        var statistics = await usageDatabase.GetStatisticsAsync();
        Console.WriteLine($"Finished getting statistics. Took {stopwatch.Elapsed}");
        _summaryTable.AppendNumber("#Indexed Features", statistics.FeatureCount);
        _summaryTable.AppendNumber("#Indexed Reference Units", statistics.ReferenceUnitCount);
        _summaryTable.AppendNumber("#Indexed Usages", statistics.UsageCount);
        _summaryTable.AppendBytes("#Index Size", databaseSize);

        Console.WriteLine("Deleting reference units without usages...");

        stopwatch.Restart();
        var referenceUnitsWithoutUsages = await usageDatabase.DeleteReferenceUnitsWithoutUsages();

        Console.WriteLine($"Finished deleting reference units without usages. Deleted {referenceUnitsWithoutUsages:N0} reference units. Took {stopwatch.Elapsed}");
        _summaryTable.AppendNumber("#Reference units without usages", referenceUnitsWithoutUsages);

        Console.WriteLine("Deleting irrelevant features...");

        stopwatch.Restart();
        var irrelevantFeatures = await usageDatabase.DeleteIrrelevantFeaturesAsync(apiCatalog);

        Console.WriteLine($"Finished deleting irrelevant features. Deleted {irrelevantFeatures:N0} features. Took {stopwatch.Elapsed}");
        _summaryTable.AppendNumber("#Irrelevant features", irrelevantFeatures);

        Console.WriteLine("Inserting parent features...");

        stopwatch.Restart();
        await usageDatabase.InsertParentsFeaturesAsync(apiCatalog);

        Console.WriteLine($"Finished inserting parent features. Took {stopwatch.Elapsed}");

        Console.WriteLine("Exporting usages...");

        stopwatch.Restart();
        await usageDatabase.ExportUsagesAsync(usagesPath);

        Console.WriteLine($"Finished exporting usages. Took {stopwatch.Elapsed}");

        Console.WriteLine("Uploading usages...");

        await _store.UploadNuGetUsageResultsAsync(usagesPath);
    }

    private async Task CrawlPackagesAsync(NuGetUsageDatabase usageDatabase, IEnumerable<PackageIdentity> packages)
    {
        var numberOfWorkers = Environment.ProcessorCount;
        Console.WriteLine($"Crawling using {numberOfWorkers} workers.");

        Console.WriteLine("::group::Crawling");

        var crawlingTimeout = TimeSpan.FromHours(5);
        using var cts = new CancellationTokenSource(crawlingTimeout);
        var cancellationToken = cts.Token;

        var inputQueue = new ConcurrentQueue<PackageIdentity>(packages);
        var outputQueue = new BlockingCollection<PackageResults>();

        var workers = Enumerable.Range(0, numberOfWorkers)
            .Select(i => Task.Run(() => CrawlWorker(i, inputQueue, outputQueue, _scratchFileProvider, cancellationToken), cancellationToken))
            .ToArray();

        var outputWorker = Task.Run(() => OutputWorker(usageDatabase, outputQueue), cancellationToken);

        await Task.WhenAll(workers);

        Console.WriteLine("::endgroup::");

        outputQueue.CompleteAdding();
        await outputWorker;

        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Crawling interrupted because timeout of {crawlingTimeout} was exceeded.");
            Console.WriteLine($"There are {inputQueue.Count:N0} items left to index.");
        }

        _summaryTable.AppendNumber("#Packages left to index", inputQueue.Count);

        static async Task CrawlWorker(int workerId,
                                      ConcurrentQueue<PackageIdentity> inputQueue,
                                      BlockingCollection<PackageResults> outputQueue,
                                      ScratchFileProvider scratchFileProvider,
                                      CancellationToken cancellationToken)
        {
            try
            {
                var fileName = scratchFileProvider.GetScratchFilePath($"worker_{workerId:000}.txt");
                File.Delete(fileName);

                while (inputQueue.TryDequeue(out var packageId))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var (exitCode, log) = await RunPackageCrawlerAsync(packageId, fileName, cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var collectionResults = File.Exists(fileName)
                            ? await CollectionSetResults.LoadAsync(fileName)
                            : CollectionSetResults.Empty;

                        var results = new PackageResults(packageId, exitCode, log, collectionResults);
                        outputQueue.Add(results, cancellationToken);

                        File.Delete(fileName);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine($"[Worker {workerId:000}] Task cancelled for package {packageId.Id} {packageId.Version}. Continuing with next package.");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Worker {workerId:000}] Error processing package {packageId.Id} {packageId.Version}: {ex.GetType().Name} - {ex.Message}");
                        // Add empty result to mark package as processed even though it failed
                        var results = new PackageResults(packageId, 1, new[] { $"Error: {ex.Message}" }, CollectionSetResults.Empty);
                        outputQueue.Add(results, cancellationToken);
                        continue;
                    }
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
                foreach (var (packageIdentity, exitCode, logLines, collectionSetResults) in queue.GetConsumingEnumerable())
                {
                    if (exitCode != 0)
                    {
                        foreach (var line in logLines)
                            Console.WriteLine($"[Crawler] {line}");
                    }

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

    private async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesAsync()
    {
        var result = await NuGetFeed.NuGetOrg.GetAllPackages();

        // The NuGet package list crawler allocates a ton of memory because it fans out pretty hard.
        // Let's make sure we're releasing as much memory as can so that the processes we're about
        // to spin up got more memory to play with.

        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        return result;
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

    private static async Task<(int ExitCode, IReadOnlyList<string> OutputLines)> RunPackageCrawlerAsync(PackageIdentity packageId, string fileName, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            ArgumentList =
            {
                "crawl-package",
                "-n",
                packageId.Id,
                "-v",
                packageId.Version.ToString(),
                "-o",
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

        await process.WaitForExitAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            process.Kill();
            processLog.Add("Crawling cancelled.");
            return (1, processLog);
        }

        processLog.Add($"Exit code = {process.ExitCode}");
        return (process.ExitCode, processLog);

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
                return;

            lock (processLogLock)
                processLog.Add(e.Data);
        }
    }

    private record PackageResults(PackageIdentity PackageIdentity, int ExitCode, IReadOnlyCollection<string> Log, CollectionSetResults CollectionSetResults);
}
