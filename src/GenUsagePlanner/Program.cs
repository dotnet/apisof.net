using System.Diagnostics;

using GenUsagePlanner.Infra;

using Microsoft.Extensions.Configuration;

using Terrajobst.ApiCatalog;

namespace GenUsagePlanner;

// TODO: Reconcile the contents of the Infra folder with Terrajobst.UsageCrawling
//       Ideally, we want to split the NuGet specific crawling functionality from
//       the generic notion of associating an API with an application/package/report.

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

        await store.DownloadDatabaseAsync(databasePath);

        using var usageDatabase = await UsageDatabase.OpenOrCreateAsync(databasePath);

        Console.WriteLine("Discovering existing APIs...");

        var apiMap = await usageDatabase.ReadApisAsync();

        Console.WriteLine("Discovering existing planner fingerprints...");

        var referenceUnitIdMap = await usageDatabase.ReadReferenceUnitsAsync();

        Console.WriteLine("Discovering latest planner fingerprints...");

        var stopwatch = Stopwatch.StartNew();

        var lastIndexed = await usageDatabase.GetAndUpdateReferenceDateAsync();
        var plannerFingerprints = await store.GetPlannerFingerprintsAsync(lastIndexed);

        Console.WriteLine($"Finished planner fingerprint discovery. Took {stopwatch.Elapsed}");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} planner fingerprints(s) to update.");

        var indexedFingerprints = new HashSet<string>(referenceUnitIdMap.Values, StringComparer.OrdinalIgnoreCase);
        var fingerprintsToBeDeleted = plannerFingerprints.Where(f => indexedFingerprints.Contains(f)).ToArray();

        Console.WriteLine($"Found {fingerprintsToBeDeleted.Length:N0} fingerprints(s) to remove from the index.");
        Console.WriteLine($"Found {plannerFingerprints.Count:N0} fingerprints(s) to add to the index.");

        Console.WriteLine($"Deleting planner fingerprints...");

        stopwatch.Restart();
        await usageDatabase.DeleteReferenceUnitsAsync(fingerprintsToBeDeleted.Select(p => referenceUnitIdMap.GetId(p)));

        Console.WriteLine($"Finished deleting planner fingerprints. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Inserting new planner fingerprints...");

        stopwatch.Restart();

        using (var referenceUnitWriter = usageDatabase.CreateReferenceUnitWriter())
        {
            foreach (var identifier in plannerFingerprints)
            {
                var referenceUnitId = referenceUnitIdMap.GetOrAdd(identifier);
                await referenceUnitWriter.WriteAsync(referenceUnitId, identifier);
            }

            await referenceUnitWriter.SaveAsync();
        }

        Console.WriteLine($"Finished inserting new planner fingerprints. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Inserting planner usages...");

        stopwatch.Restart();

        using (var writer = usageDatabase.CreateUsageWriter())
        {
            foreach (var fingerprint in plannerFingerprints)
            {
                var referenceUnitId = referenceUnitIdMap.GetId(fingerprint);
                var apis = await store.GetPlannerApisAsync(fingerprint);

                foreach (var api in apis)
                {
                    var apiId = apiMap.GetOrAdd(api);
                    await writer.WriteAsync(referenceUnitId, apiId);
                }
            }

            await writer.SaveAsync();
        }

        Console.WriteLine($"Finished inserting planner usages. Took {stopwatch.Elapsed}");

        Console.WriteLine("Inserting missing APIs...");

        stopwatch.Restart();
        await usageDatabase.InsertMissingApisAsync(apiMap);

        Console.WriteLine($"Finished inserting missing APIs. Took {stopwatch.Elapsed}");

        Console.WriteLine($"Vacuuming database...");

        stopwatch.Restart();
        await usageDatabase.VacuumAsync();

        Console.WriteLine($"Finished vacuuming database. Took {stopwatch.Elapsed}");

        await usageDatabase.CloseAsync();

        Console.WriteLine($"Uploading database...");

        await store.UploadDatabaseAsync(databasePath);

        await usageDatabase.OpenAsync();

        Console.WriteLine($"Aggregating results...");

        stopwatch.Restart();

        var ancestors = apiCatalog.GetAllApis()
                                  .SelectMany(a => a.AncestorsAndSelf(), (api, ancestor) => (api.Guid, ancestor.Guid));
        await usageDatabase.InsertApiAncestorsAndExportUsagesAsync(apiMap, ancestors, usagesPath);

        Console.WriteLine($"Finished aggregating results. Took {stopwatch.Elapsed}");

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
