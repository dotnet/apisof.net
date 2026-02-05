using ApisOfDotNet.Shared;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace ApisOfDotNet.Services;

public sealed class BlobStorageService
{
    private readonly BlobServiceClient _serviceClient;

    public BlobStorageService(IOptions<ApisOfDotNetOptions> options)
    {
        ThrowIfNull(options);

        var serviceUri = new Uri(options.Value.AzureStorageServiceUrl);
#if DEBUG
        TokenCredential credential = new DefaultAzureCredential();
#else
        TokenCredential credential = new ManagedIdentityCredential();
#endif
        _serviceClient = new BlobServiceClient(serviceUri, credential);
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
