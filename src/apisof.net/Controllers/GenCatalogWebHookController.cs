using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        ThrowIfNull(configuration);
        ThrowIfNull(catalogService);

        _configuration = configuration;
        _catalogService = catalogService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        string secret;
        using (var reader = new StreamReader(Request.Body))
            secret = (await reader.ReadToEndAsync()).Trim();

        var expectedSecret = _configuration["GenCatalogWebHookSecret"].Trim();
        var matches = FixedTimeComparer.Equals(secret, expectedSecret);

        if (!matches)
            return Unauthorized();

        await _catalogService.InvalidateAsync();

        return Ok();
    }
}