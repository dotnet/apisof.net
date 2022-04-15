using Azure.Core;
using Azure.Storage.Blobs;

namespace Terrajobst.UsageCrawling;

public sealed class BlobStorageCrawlerStore : CrawlerStore
{
    private const string CatalogContainerName = "catalog";
    private const string UsageContainerName = "usage";

    private readonly string _blobStorageConnectionString;

    public BlobStorageCrawlerStore(string blobStorageConnectionString)
    {
        _blobStorageConnectionString = blobStorageConnectionString;
    }

    private async Task EnsureContainerExist()
    {
        var client = new BlobContainerClient(_blobStorageConnectionString, UsageContainerName, GetBlobOptions());
        await client.CreateIfNotExistsAsync();
    }

    public override Task DownloadApiCatalogAsync(string fileName)
    {
        var blobClient = new BlobClient(_blobStorageConnectionString, CatalogContainerName, ApiCatalogName, GetBlobOptions());
        return blobClient.DownloadToAsync(fileName);
    }

    public override async Task<bool> DownloadDatabaseAsync(string fileName)
    {
        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        if (!await blobClient.ExistsAsync())
            return false;

        await blobClient.DownloadToAsync(fileName);
        return true;
    }

    public override async Task UploadDatabaseAsync(string fileName)
    {
        await EnsureContainerExist();

        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true);
    }

    public override async Task UploadResultsAsync(string fileName)
    {
        await EnsureContainerExist();

        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, UsagesName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true);
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