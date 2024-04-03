using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("/catalog/download")]
[AllowAnonymous]
public sealed class DownloadController : Controller
{
    private readonly IConfiguration _configuration;

    public DownloadController(IConfiguration configuration)
    {
        ThrowIfNull(configuration);

        _configuration = configuration;
    }

    [HttpGet]
    public async Task<FileStreamResult> Get()
    {
        var azureConnectionString = _configuration["AzureStorageConnectionString"];
        var blobClient = new BlobClient(azureConnectionString, "catalog", "apicatalog.dat");
        var stream = await blobClient.OpenReadAsync();
        return new FileStreamResult(stream, "application/octet-stream") {
            FileDownloadName = "apicatalog.dat"
        };
    }
}