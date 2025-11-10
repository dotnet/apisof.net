namespace ApisOfDotNet.Services;

public sealed class CatalogServiceWarmUp : IHostedService
{
    private readonly CatalogService _catalogService;

    public CatalogServiceWarmUp(CatalogService catalogService)
    {
        ThrowIfNull(catalogService);

        _catalogService = catalogService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _catalogService.InvalidateAsync();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
