using System.Threading.RateLimiting;
using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("/catalog/download")]
[AllowAnonymous]
public sealed class DownloadController : Controller
{
    private readonly BlobStorageService _blobStorageService;

    public DownloadController(BlobStorageService blobStorageService)
    {
        ThrowIfNull(blobStorageService);

        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    public async Task<FileStreamResult> Get()
    {
        var blobClient = _blobStorageService.GetBlobClient("catalog", "apicatalog.dat");
        var stream = await blobClient.OpenReadAsync();
        return new FileStreamResult(stream, "application/octet-stream")
        {
            FileDownloadName = "apicatalog.dat"
        };
    }
}