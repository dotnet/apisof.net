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
        Console.WriteLine($"Initializing BlobStorageService with service URL: '{options.Value.AzureStorageServiceUrl}'");
        var serviceUrl = options.Value.AzureStorageServiceUrl.TrimEnd('/');
        var serviceUri = new Uri(serviceUrl);
        TokenCredential credential = new ManagedIdentityCredential();

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
