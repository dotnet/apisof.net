using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("webhook")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public sealed class WebHookController : Controller
{
    private readonly IOptions<ApisOfDotNetOptions> _options;
    private readonly CatalogService _catalogService;
    private readonly ILogger<WebHookController> _logger;

    public WebHookController(IOptions<ApisOfDotNetOptions> options,
                             CatalogService catalogService,
                             ILogger<WebHookController> logger)
    {
        ThrowIfNull(options);
        ThrowIfNull(catalogService);
        ThrowIfNull(logger);

        _options = options;
        _catalogService = catalogService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post(string subject)
    {
        string secret;
        using (var reader = new StreamReader(Request.Body))
            secret = (await reader.ReadToEndAsync()).Trim();

        var expectedSecret = _options.Value.ApisOfDotNetWebHookSecret.Trim();
        var matches = FixedTimeComparer.Equals(secret, expectedSecret);

        if (!matches)
        {
            _logger.LogInformation($"Received blob change with invalid secret.");
            return Unauthorized();
        }

        var wellKnownBlob = IdentifyBlob(subject);

        if (wellKnownBlob is null)
            _logger.LogInformation($"Received blob change for unknown subject '{subject}'");
        else
            _logger.LogInformation($"Received blob change for {wellKnownBlob} ('{subject}')");

        switch (wellKnownBlob)
        {
            case WellKnownBlob.ApiCatalog:
                _catalogService.InvalidateCatalog();
                break;
            case WellKnownBlob.DesignNotes:
                _catalogService.InvalidateDesignNotes();
                break;
            case WellKnownBlob.UsageData:
                _catalogService.InvalidateUsageData();
                break;
        }

        return Ok();
    }

    private static WellKnownBlob? IdentifyBlob(string? subject)
    {
        if (subject is null)
            return null;

        if (subject.EndsWith("job.json", StringComparison.OrdinalIgnoreCase))
            return WellKnownBlob.ApiCatalog;
        else if (subject.EndsWith("designNotes.dat", StringComparison.OrdinalIgnoreCase))
            return WellKnownBlob.DesignNotes;
        else if (subject.EndsWith("usageData.dat", StringComparison.OrdinalIgnoreCase))
            return WellKnownBlob.UsageData;
        else
            return null;
    }

    private enum WellKnownBlob
    {
        ApiCatalog,
        DesignNotes,
        UsageData
    }
}