namespace ApisOfDotNet.Services;

public sealed class CatalogServiceWarmUp : IHostedService
{
    private readonly CatalogService _catalogService;

    public CatalogServiceWarmUp(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _catalogService.InvalidateAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}