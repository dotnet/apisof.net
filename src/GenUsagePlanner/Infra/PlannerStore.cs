using Azure.Core;
using Azure.Storage.Blobs;

namespace GenUsagePlanner.Infra;

public sealed class PlannerStore
{
    private const string CatalogContainerName = "catalog";
    private const string UsageContainerName = "usage";
    private const string PlannerContainerName = "planner";

    private const string ApiCatalogName = "apicatalog.dat";
    private const string DatabaseName = "usages-planner.db";
    private const string UsagesName = "usages-planner.tsv";

    private readonly string _connectionString;

    public PlannerStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task EnsureContainerExist()
    {
        var client = new BlobContainerClient(_connectionString, UsageContainerName, GetBlobOptions());
        await client.CreateIfNotExistsAsync();
    }

    public Task DownloadApiCatalogAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, CatalogContainerName, ApiCatalogName, GetBlobOptions());
        return blobClient.DownloadToAsync(fileName);
    }

    public async Task<bool> DownloadDatabaseAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        if (!await blobClient.ExistsAsync())
            return false;

        await blobClient.DownloadToAsync(fileName);
        return true;
    }

    public async Task UploadDatabaseAsync(string fileName)
    {
        await EnsureContainerExist();

        var blobClient = new BlobClient(_connectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true);
    }

    public async Task UploadResultsAsync(string fileName)
    {
        await EnsureContainerExist();

        var blobClient = new BlobClient(_connectionString, UsageContainerName, UsagesName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true);
    }

    public async Task<IReadOnlyList<string>> GetPlannerFingerprintsAsync(DateTimeOffset? since = null)
    {
        var result = new List<string>();
        var containerClient = new BlobContainerClient(_connectionString, PlannerContainerName, GetBlobOptions());

        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            if (since is not null && blob.Properties.CreatedOn < since)
                continue;

            var fingerprint = blob.Name;
            result.Add(fingerprint);
        }

        return result;
    }

    public async Task<IReadOnlyList<Guid>> GetPlannerApisAsync(string fingerprint)
    {
        var client = new BlobClient(_connectionString, PlannerContainerName, fingerprint, GetBlobOptions());
        var content = await client.DownloadContentAsync();

        var result = new List<Guid>();
        using var reader = new StreamReader(content.Value.Content.ToStream());
        while (reader.ReadLine() is string line)
            if (Guid.TryParse(line, out var guid))
                result.Add(guid);

        return result;
    }

    private static BlobClientOptions GetBlobOptions()
    {
        return new BlobClientOptions
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(90),
                MaxRetries = 10,
                NetworkTimeout = TimeSpan.FromMinutes(5),
            }
        };
    }
}
