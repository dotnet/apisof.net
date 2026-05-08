using ApisOfDotNet.Shared;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace ApisOfDotNet.Services;

public sealed class BlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<ApisOfDotNetOptions> options, ILogger<BlobStorageService> logger)
    {
        ThrowIfNull(options);
        ThrowIfNull(logger);

        _logger = logger;

        var serviceUri = new Uri(options.Value.AzureStorageServiceUrl);
        _logger.LogInformation("Initializing BlobStorageService with endpoint: {ServiceUri}", serviceUri);
        _serviceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
        _logger.LogInformation("BlobStorageService initialized successfully.");
    }

    public BlobServiceClient GetServiceClient() => _serviceClient;

    public BlobContainerClient GetContainerClient(string containerName)
    {
        ThrowIfNullOrEmpty(containerName);
        return _serviceClient.GetBlobContainerClient(containerName);
    }

    public BlobClient GetBlobClient(string containerName, string blobName)
    {
        ThrowIfNullOrEmpty(containerName);
        ThrowIfNullOrEmpty(blobName);

        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        return containerClient.GetBlobClient(blobName);
    }
}
