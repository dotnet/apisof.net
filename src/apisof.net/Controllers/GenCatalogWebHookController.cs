using System.IO;
using System.Threading.Tasks;
using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("gencatalog-webhook")]
[AllowAnonymous]
public sealed class GenCatalogWebHookController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly CatalogService _catalogService;

    public GenCatalogWebHookController(IConfiguration configuration, CatalogService catalogService)
    {
        _configuration = configuration;
        _catalogService = catalogService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        string secret;
        using (var reader = new StreamReader(Request.Body))
            secret = await reader.ReadToEndAsync();

        if (secret != null)
            secret.Trim();

        var expectedSecret = _configuration["GenCatalogWebHookSecret"].Trim();

        if (secret != expectedSecret)
            return Unauthorized();

        await _catalogService.InvalidateAsync();

        return Ok();
    }
}