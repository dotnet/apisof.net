using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Constants
const string StorageAccountUrl = "https://apisofdotnet.blob.core.windows.net";
const string DownloadPath = "downloads";

// Build host with DI
var builder = Host.CreateApplicationBuilder(args);

// Register BlobServiceClient as singleton with retry policy
builder.Services.AddSingleton(sp =>
{
    var serviceUri = new Uri(StorageAccountUrl);
    TokenCredential credential = new ManagedIdentityCredential();

    return new BlobServiceClient(serviceUri, credential, new BlobClientOptions
    {
        Retry =
        {
            Mode = Azure.Core.RetryMode.Exponential,
            Delay = TimeSpan.FromSeconds(90),
            MaxRetries = 10,
            NetworkTimeout = TimeSpan.FromMinutes(5)
        }
    });
});

var host = builder.Build();
var blobServiceClient = host.Services.GetRequiredService<BlobServiceClient>();

// Download logic
int totalContainers = 0;
int totalFiles = 0;
long totalBytes = 0;
int errorCount = 0;

Console.WriteLine("Enumerating containers...");

var containers = new List<BlobContainerItem>();
await foreach (var container in blobServiceClient.GetBlobContainersAsync())
{
    containers.Add(container);
}

totalContainers = containers.Count;
Console.WriteLine($"Found {totalContainers} containers");
Console.WriteLine();

for (int i = 0; i < containers.Count; i++)
{
    var container = containers[i];
    var containerName = container.Name;
    int containerIndex = i + 1;

    Console.WriteLine($"[{containerIndex}/{totalContainers}] Processing container: {containerName}");

    try
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        int fileCount = 0;
        long containerBytes = 0;

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var blobName = blobItem.Name;
            var blobSize = blobItem.Properties.ContentLength ?? 0;

            try
            {
                // Create local path preserving blob directory structure
                var localPath = Path.Combine(DownloadPath, containerName, blobName.Replace('/', Path.DirectorySeparatorChar));
                var directory = Path.GetDirectoryName(localPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Console.WriteLine($"  Downloading: {blobName} ({blobSize:N0} bytes)");

                // Stream download to file
                var blobClient = containerClient.GetBlobClient(blobName);
                await using var fileStream = File.Create(localPath);
                await blobClient.DownloadToAsync(fileStream);

                fileCount++;
                containerBytes += blobSize;
                totalFiles++;
                totalBytes += blobSize;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR: {blobName} - {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine($"  Completed {containerName}: {fileCount} files, {containerBytes:N0} bytes");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR: Failed to process container {containerName} - {ex.Message}");
        errorCount++;
    }

    Console.WriteLine();
}

Console.WriteLine($"Download complete: {totalContainers} containers, {totalFiles} files, {totalBytes:N0} bytes, {errorCount} errors");

if (errorCount > 0)
{
    Environment.Exit(1);
}