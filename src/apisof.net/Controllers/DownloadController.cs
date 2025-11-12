using System.Threading.RateLimiting;
using ApisOfDotNet.Shared;
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
    private readonly AzureBlobClientManager _blobClientManager;


    public DownloadController(IOptions<ApisOfDotNetOptions> options, AzureBlobClientManager azureBlobClientManager)
    {
        ThrowIfNull(options);
        ThrowIfNull(azureBlobClientManager);
        _blobClientManager = azureBlobClientManager;
        _options = options;
    }

    [HttpGet]
    public async Task<FileStreamResult> Get()
    { 
        var containerClient = _blobClientManager.GetBlobContainerClient("catalog");
        var stream = await containerClient.GetBlobClient("apicatalog.dat").OpenReadAsync();

        return new FileStreamResult(stream, "application/octet-stream") {
            FileDownloadName = "apicatalog.dat"
        };
    }
}