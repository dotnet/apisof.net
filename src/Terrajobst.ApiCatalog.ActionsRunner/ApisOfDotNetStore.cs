using ApisOfDotNet.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class ApisOfDotNetStore
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<ApisOfDotNetStoreOptions> _options;
    private readonly ILogger<ApisOfDotNetStore> _logger;
    private readonly AzureBlobClientManager _blobClientManager;

    private const string BlobMetadataKeyIndexTimestamp = "IndexTimestamp";

    public ApisOfDotNetStore(IHostEnvironment hostEnvironment,
                             IOptions<ApisOfDotNetStoreOptions> options,
                             ILogger<ApisOfDotNetStore> logger,
                             AzureBlobClientManager blobClientManager)
    {
        ThrowIfNull(options);
        ThrowIfNull(logger);
        ThrowIfNull(blobClientManager);

        _hostEnvironment = hostEnvironment;
        _options = options;
        _logger = logger;
        _blobClientManager = blobClientManager;
    }

    public async Task SetTimestampAsync(string blobContainer, string blobName, DateTimeOffset timestamp)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            Console.WriteLine($"Setting timestamp for {blobContainer}/{blobName} suppressed for development.");
            return;
        }

        Console.WriteLine($"Setting timestamp for {blobContainer}/{blobName}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);
        await blobClient.SetMetadataAsync(new Dictionary<string, string> {
            [BlobMetadataKeyIndexTimestamp] = timestamp.ToString("O")
        });
    }

    public async Task<DateTimeOffset?> GetTimestampAsync(string blobContainer, string blobName)
    {
        Console.WriteLine($"Getting timestamp for {blobContainer}/{blobName}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);

        var properties = await blobClient.GetPropertiesAsync();
        if (properties.HasValue &&
            properties.Value.Metadata.TryGetValue(BlobMetadataKeyIndexTimestamp, out var timestampText) &&
            DateTimeOffset.TryParse(timestampText, out var dateTimeOffset))
        {
            return dateTimeOffset;
        }

        return null;
    }

    public async Task UploadAsync(string blobContainer, string blobName, string fileName)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            Console.WriteLine($"Upload of {blobContainer}/{blobName} suppressed for development.");
            return;
        }

        Console.WriteLine($"Uploading {blobContainer}/{blobName}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);
        await blobClient.UploadAsync(fileName, overwrite: true);
    }

    public async Task UploadAsync(string blobContainer, string blobName, Stream stream)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            Console.WriteLine($"Upload of {blobContainer}/{blobName} suppressed for development.");
            return;
        }

        Console.WriteLine($"Uploading {blobContainer}/{blobName}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<IReadOnlyList<string>> GetBlobNamesAsync(string blobContainer,  DateTimeOffset? since = null)
    {
        Console.WriteLine($"Enumerating {blobContainer}...");

        var containerClient = _blobClientManager.GetBlobContainerClient(blobContainer);

        var result = new List<string>();
        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            if (since is not null && blob.Properties.LastModified < since)
                continue;

            result.Add(blob.Name);
        }

        return result.ToArray();
    }

    public async Task DownloadToAsync(string blobContainer, string blobName, string fileName)
    {
        Console.WriteLine($"Downloading {Path.GetFileName(fileName)}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(fileName);
        File.SetLastWriteTimeUtc(fileName, lastModified.UtcDateTime);
    }

    public async Task<Stream> OpenReadAsync(string blobContainer, string blobName)
    {
        Console.WriteLine($"Opening blob {blobContainer}/{blobName}...");

        var blobClient = _blobClientManager.GetBlobContainerClient(blobContainer).GetBlobClient(blobName);
        return await blobClient.OpenReadAsync();
    }
}