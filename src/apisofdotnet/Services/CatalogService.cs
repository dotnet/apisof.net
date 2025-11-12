using ApisOfDotNet.Shared;
using Terrajobst.ApiCatalog;

internal sealed class CatalogService
{
    private readonly AzureBlobClientManager _blobClientManager;

    public CatalogService(AzureBlobClientManager blobClientManager)
    {
        _blobClientManager = blobClientManager;
    }

    public async Task<ApiCatalogModel> LoadCatalogAsync()
    {
        var catalogPath = GetCatalogPath();

        if (!File.Exists(catalogPath))
        {
            await DownloadCatalogAsync();
        }

        try
        {
            return await ApiCatalogModel.LoadAsync(catalogPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: can't open catalog: {ex.Message}");
            Environment.Exit(1);
            return null;
        }
    }

    private static string GetCatalogPath()
    {
        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        var catalogPath = Path.Join(processDirectory, "apicatalog.db");
        return catalogPath;
    }

    public async Task DownloadCatalogAsync(bool force = false)
    {
        var catalogPath = GetCatalogPath();
        var containerClient = _blobClientManager.GetBlobContainerClient("catalog");
        var blobClient = containerClient.GetBlobClient("apicatalog.db");

        if (!force && File.Exists(catalogPath))
        {
            Console.WriteLine("Checking catalog...");
            var localTimestamp = File.GetLastWriteTimeUtc(catalogPath);
            var properties = await blobClient.GetPropertiesAsync();
            var blobTimestamp = properties.Value.LastModified.UtcDateTime;
            var blobIsNewer = blobTimestamp > localTimestamp;

            if (!blobIsNewer)
            {
                Console.WriteLine("Catalog is up-to-date.");
                return;
            }
        }

        try
        {
            Console.WriteLine("Downloading catalog...");
            await blobClient.DownloadToAsync(catalogPath);
            var properties = await blobClient.GetPropertiesAsync();
            File.SetLastWriteTimeUtc(catalogPath, properties.Value.LastModified.UtcDateTime);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: can't download catalog: {ex.Message}");
            Environment.Exit(1);
        }
    }
}