using System.Diagnostics;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.ActionsRunner;
using Terrajobst.UsageCrawling.Storage;

namespace GenUsagePlanner;

internal sealed class Main : IConsoleMain
{
    private readonly ScratchFileProvider _scratchFileProvider;
    private readonly ApisOfDotNetStore _store;
    private readonly GitHubActionsSummaryTable _summaryTable;

    public Main(ScratchFileProvider scratchFileProvider,
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

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var apiCatalogPath = _scratchFileProvider.GetScratchFilePath("apicatalog.dat");
        var databasePath = _scratchFileProvider.GetScratchFilePath("usage-planner.db");
        var usagesPath = _scratchFileProvider.GetScratchFilePath("usages-planner.tsv");

        Console.WriteLine("Downloading API catalog...");

        await _store.DownloadApiCatalogAsync(apiCatalogPath);

        Console.WriteLine("Loading API catalog...");

        var apiCatalog = await ApiCatalogModel.LoadAsync(apiCatalogPath);

        Console.WriteLine("Downloading previously indexed usages...");

        var (_, lastIndexTimestamp) = await _store.DownloadPlannerUsageDatabaseAsync(databasePath);

        using var usageDatabase = await UsageDatabase.OpenOrCreateAsync(databasePath);

        Console.WriteLine("Discovering existing planner fingerprints...");

        var referenceUnits = (await usageDatabase.GetReferenceUnitsAsync()).Select(r => r.Identifier).ToArray();
        Console.WriteLine($"Found {referenceUnits.Length:N0} planner fingerprints in the index.");
        _summaryTable.AppendNumber("#Fingerprints in index", referenceUnits.Length);

        Console.WriteLine("Discovering latest planner fingerprints...");

        var stopwatch = Stopwatch.StartNew();

        var indexTimestamp = DateTime.UtcNow;
        var plannerFingerprints = await _store.GetPlannerFingerprintsAsync(lastIndexTimestamp);

        Console.WriteLine($"Finished planner fingerprint discovery. Took {stopwatch.Elapsed}");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} planner fingerprints(s) to update.");

        var indexedFingerprints = new HashSet<string>(referenceUnits, StringComparer.OrdinalIgnoreCase);
        var fingerprintsToBeDeleted = plannerFingerprints.Where(f => indexedFingerprints.Contains(f)).ToArray();

        Console.WriteLine($"Found {fingerprintsToBeDeleted.Length:N0} fingerprints(s) to remove from the index.");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} fingerprints(s) to add to the index.");
        _summaryTable.AppendNumber("#Fingerprints to be deleted", fingerprintsToBeDeleted.Length);
        _summaryTable.AppendNumber("#Fingerprints to be added", plannerFingerprints.Count);

        Console.WriteLine($"Deleting planner fingerprints...");

        stopwatch.Restart();
        await usageDatabase.DeleteReferenceUnitsAsync(fingerprintsToBeDeleted);

        Console.WriteLine($"Finished deleting planner fingerprints. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Inserting new planner fingerprints...");

        stopwatch.Restart();

        foreach (var fingerprint in plannerFingerprints)
        {
            await usageDatabase.DeleteReferenceUnitsAsync([fingerprint]);
            await usageDatabase.AddReferenceUnitAsync(fingerprint);
            var apis = await _store.GetPlannerApisAsync(fingerprint);

            foreach (var api in apis)
            {
                await usageDatabase.TryAddFeatureAsync(api);
                await usageDatabase.AddUsageAsync(fingerprint, api);
            }
        }

        Console.WriteLine($"Finished inserting new planner fingerprints. Took {stopwatch.Elapsed}");

        Console.WriteLine("Vacuuming database...");

        stopwatch.Restart();
        await usageDatabase.VacuumAsync();

        Console.WriteLine($"Finished vacuuming database. Took {stopwatch.Elapsed}");

        await usageDatabase.CloseAsync();

        Console.WriteLine("Uploading database...");

        await _store.UploadPlannerUsageDatabaseAsync(databasePath, indexTimestamp);

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

        Console.WriteLine("Deleting irrelevant features...");

        stopwatch.Restart();
        var irrelevantFeatures = await usageDatabase.DeleteIrrelevantFeaturesAsync(apiCatalog);
        _summaryTable.AppendNumber("#Irrelevant features", irrelevantFeatures);

        Console.WriteLine($"Finished deleting irrelevant features. Took {stopwatch.Elapsed}");

        Console.WriteLine("Inserting parent features...");

        stopwatch.Restart();
        await usageDatabase.InsertParentsFeaturesAsync(apiCatalog);

        Console.WriteLine($"Finished inserting parent features. Took {stopwatch.Elapsed}");

        Console.WriteLine("Exporting usages...");

        stopwatch.Restart();
        await usageDatabase.ExportUsagesAsync(usagesPath);

        Console.WriteLine($"Finished exporting usages. Took {stopwatch.Elapsed}");

        Console.WriteLine("Uploading usages...");

        await _store.UploadPlannerUsageResultsAsync(usagesPath);
    }
}