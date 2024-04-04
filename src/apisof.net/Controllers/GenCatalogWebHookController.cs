using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("gencatalog-webhook")]
[AllowAnonymous]
public sealed class GenCatalogWebHookController : Controller
{
    private readonly IOptions<ApisOfDotNetOptions> _options;
    private readonly CatalogService _catalogService;

    public GenCatalogWebHookController(IOptions<ApisOfDotNetOptions> options, CatalogService catalogService)
    {
        ThrowIfNull(options);
        ThrowIfNull(catalogService);

        _options = options;
        _catalogService = catalogService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        string secret;
        using (var reader = new StreamReader(Request.Body))
            secret = (await reader.ReadToEndAsync()).Trim();

        var expectedSecret = _options.Value.GenCatalogWebHookSecret.Trim();
        var matches = FixedTimeComparer.Equals(secret, expectedSecret);

        if (!matches)
            return Unauthorized();

        await _catalogService.InvalidateAsync();

        return Ok();
    }
}