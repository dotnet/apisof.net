using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Storage.Blobs;
namespace ApisOfDotNet.Shared
{
    public sealed class AzureBlobClientManager
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient blobServiceClient;
        private readonly ConcurrentDictionary<string, BlobContainerClient> containerClients = new(); 

        public AzureBlobClientManager(IConfiguration configuration)
        {
            _configuration = configuration;
#if DEBUG
            var credential = new DefaultAzureCredential();
#else
            var credential = new ManagedIdentityCredential();
#endif
            string blobStorageUrl = _configuration["AzureBlobStorageUri"];
            blobServiceClient = new BlobServiceClient(new Uri(blobStorageUrl), credential);
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return containerClients.GetOrAdd(containerName, name =>
            {
                return blobServiceClient.GetBlobContainerClient(name);
            });
        } 
    }
}
