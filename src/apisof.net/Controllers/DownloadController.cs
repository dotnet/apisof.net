using System.Threading.RateLimiting;
using ApisOfDotNet.Shared;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("/catalog/download")]
[AllowAnonymous]
public sealed class DownloadController : Controller
{
    private readonly IOptions<ApisOfDotNetOptions> _options;

    public DownloadController(IOptions<ApisOfDotNetOptions> options)
    {
        ThrowIfNull(options);

        _options = options;
    }

    [HttpGet]
    public async Task<FileStreamResult> Get()
    {
        var serviceUri = new Uri(_options.Value.AzureStorageServiceUrl);
#if DEBUG
        TokenCredential credential = new DefaultAzureCredential();
#else
        TokenCredential credential = new ManagedIdentityCredential();
#endif
        var serviceClient = new BlobServiceClient(serviceUri, credential);
        var containerClient = serviceClient.GetBlobContainerClient("catalog");
        var blobClient = containerClient.GetBlobClient("apicatalog.dat");
        var stream = await blobClient.OpenReadAsync();
        return new FileStreamResult(stream, "application/octet-stream") {
            FileDownloadName = "apicatalog.dat"
        };
    }
}