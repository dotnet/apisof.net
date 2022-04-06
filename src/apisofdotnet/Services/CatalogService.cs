
using Azure.Storage.Blobs;

using Terrajobst.ApiCatalog;

internal sealed class CatalogService
{
    public ApiCatalogModel LoadCatalog()
    {
        var catalogPath = GetCatalogPath();

        if (!File.Exists(catalogPath))
        {
            DownloadCatalog();
        }

        try
        {
            return ApiCatalogModel.Load(catalogPath);
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

    public void DownloadCatalog(bool force = false)
    {
        var catalogPath = GetCatalogPath();

        var url = "https://apicatalogblob.blob.core.windows.net/catalog/apicatalog.dat";
        var blobClient = new BlobClient(new Uri(url));

        if (!force && File.Exists(catalogPath))
        {
            Console.WriteLine("Checking catalog...");
            var localTimetamp = File.GetLastWriteTimeUtc(catalogPath);
            var properties = blobClient.GetProperties();
            var blobTimestamp = properties.Value.LastModified.UtcDateTime;
            var blobIsNewer = blobTimestamp > localTimetamp;

            if (!blobIsNewer)
            {
                Console.WriteLine("Catalog is up-to-date.");
                return;
            }
        }

        try
        {
            Console.WriteLine("Downloading catalog...");
            blobClient.DownloadTo(catalogPath);
            var properties = blobClient.GetProperties();
            File.SetLastWriteTimeUtc(catalogPath, properties.Value.LastModified.UtcDateTime);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: can't download catalog: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
