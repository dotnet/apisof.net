namespace ApisOfDotNet.Services;

public sealed class CatalogServiceWarmUp : IHostedService
{
    private readonly CatalogService _catalogService;

    public CatalogServiceWarmUp(CatalogService catalogService)
    {
        ThrowIfNull(catalogService);

        _catalogService = catalogService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _catalogService.InvalidateAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
