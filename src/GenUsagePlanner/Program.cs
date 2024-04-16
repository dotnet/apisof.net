using System.Diagnostics;

using GenUsagePlanner.Infra;

using Microsoft.Extensions.Configuration;

using Terrajobst.ApiCatalog;
using Terrajobst.UsageCrawling.Storage;

namespace GenUsagePlanner;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            return await MainAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task<int> MainAsync(string[] args)
    {
        var connectionString = GetAzureStorageConnectionString();
        var store = new PlannerStore(connectionString);

        var apiCatalogPath = GetScratchFilePath("apicatalog.dat");
        var databasePath = GetScratchFilePath("usage-planner.db");
        var usagesPath = GetScratchFilePath("usages-planner.tsv");

        Console.WriteLine("Downloading API catalog...");

        await store.DownloadApiCatalogAsync(apiCatalogPath);

        Console.WriteLine("Loading API catalog...");

        var apiCatalog = await ApiCatalogModel.LoadAsync(apiCatalogPath);

        Console.WriteLine("Downloading previously indexed usages...");

        var (_, lastIndexTimestamp) = await store.DownloadDatabaseAsync(databasePath);

        using var usageDatabase = await UsageDatabase.OpenOrCreateAsync(databasePath);

        Console.WriteLine("Discovering existing planner fingerprints...");

        var referenceUnits = (await usageDatabase.GetReferenceUnitsAsync()).Select(r => r.Identifier);

        Console.WriteLine("Discovering latest planner fingerprints...");

        var stopwatch = Stopwatch.StartNew();

        var indexTimestamp = DateTime.UtcNow;
        var plannerFingerprints = await store.GetPlannerFingerprintsAsync(lastIndexTimestamp);

        Console.WriteLine($"Finished planner fingerprint discovery. Took {stopwatch.Elapsed}");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} planner fingerprints(s) to update.");

        var indexedFingerprints = new HashSet<string>(referenceUnits, StringComparer.OrdinalIgnoreCase);
        var fingerprintsToBeDeleted = plannerFingerprints.Where(f => indexedFingerprints.Contains(f)).ToArray();

        Console.WriteLine($"Found {fingerprintsToBeDeleted.Length:N0} fingerprints(s) to remove from the index.");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} fingerprints(s) to add to the index.");

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
            var apis = await store.GetPlannerApisAsync(fingerprint);

            foreach (var api in apis)
            {
                await usageDatabase.TryAddFeatureAsync(api);
                await usageDatabase.AddUsageAsync(fingerprint, api);
            }
        }

        Console.WriteLine($"Finished inserting new planner fingerprints. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Vacuuming database...");

        stopwatch.Restart();
        await usageDatabase.VacuumAsync();

        Console.WriteLine($"Finished vacuuming database. Took {stopwatch.Elapsed}");

        await usageDatabase.CloseAsync();

        Console.WriteLine($"Uploading database...");

        await store.UploadDatabaseAsync(databasePath, indexTimestamp);

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

        await store.UploadResultsAsync(usagesPath);

        return 0;
    }

    private static string GetAzureStorageConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config["AzureStorageConnectionString"];
        if (connectionString is null)
        {
            Console.Error.WriteLine("error: can't find configuration for AzureStorageConnectionString");
            Environment.Exit(1);
        }

        return connectionString;
    }

    private static string GetScratchFilePath(string fileName)
    {
        var path = Path.Join(Environment.CurrentDirectory, "scratch", fileName);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        return path;
    }
}
