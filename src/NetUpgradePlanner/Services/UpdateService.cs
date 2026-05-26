using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Velopack;

namespace NetUpgradePlanner.Services;

internal sealed class UpdateService : BackgroundService
{
    private readonly string _storageUrl;
    private readonly ProgressService _progressService;

    public bool HasUpdate { get; private set; }

    public UpdateService(ProgressService progressService, IConfiguration configuration)
    {
        _progressService = progressService;
        _storageUrl = configuration["Environment:BaseUrl"] + "/squirrel";
    }

    public async Task<bool> CheckForUpdateAsync()
    {
        var hasUpdate = await Task.Run(async () =>
        {
            var updateManager = new UpdateManager(_storageUrl);
            if (!updateManager.IsInstalled)
                return false;

            var result = await updateManager.CheckForUpdatesAsync();
            return result is not null;
        });

        HasUpdate = hasUpdate;
        Changed?.Invoke(this, EventArgs.Empty);

        return hasUpdate;
    }

    public async Task UpdateAsync()
    {
        await _progressService.Run(async pm =>
        {
            var mgr = new UpdateManager(_storageUrl);
            var update = await mgr.CheckForUpdatesAsync();

            if (update is null)
                return;

            await mgr.DownloadUpdatesAsync(update, p => pm.Report(p, 100));
            mgr.ApplyUpdatesAndRestart(update);
        }, "Updating application");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CheckForUpdateAsync();
    }

    public event EventHandler? Changed;
}
