namespace ApisOfDotNet.Services;

public sealed class CatalogServiceWarmUp : IHostedService
{
    private readonly CatalogService _catalogService;
    private readonly ILogger<CatalogServiceWarmUp> _logger;

    public CatalogServiceWarmUp(CatalogService catalogService, ILogger<CatalogServiceWarmUp> logger)
    {
        ThrowIfNull(catalogService);
        ThrowIfNull(logger);

        _catalogService = catalogService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = WarmUpAsync();
        return Task.CompletedTask;
    }

    private async Task WarmUpAsync()
    {
        try
        {
            await _catalogService.InvalidateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm up catalog service.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
