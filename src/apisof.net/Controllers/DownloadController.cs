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

    public DownloadController(IOptions<ApisOfDotNetOptions> options)
    {
        ThrowIfNull(_options);

        _options = options;
    }

    [HttpGet]
    public async Task<FileStreamResult> Get()
    {
        var azureConnectionString = _options.Value.AzureStorageConnectionString;
        var blobClient = new BlobClient(azureConnectionString, "catalog", "apicatalog.dat");
        var stream = await blobClient.OpenReadAsync();
        return new FileStreamResult(stream, "application/octet-stream") {
            FileDownloadName = "apicatalog.dat"
        };
    }
}