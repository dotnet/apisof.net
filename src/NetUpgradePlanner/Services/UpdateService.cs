using Microsoft.Extensions.Hosting;

using Squirrel;

namespace NetUpgradePlanner.Services;

internal sealed class UpdateService : BackgroundService
{
    private const string _storageUrl = "https://apisofdotnet.blob.core.windows.net/squirrel";
    private readonly ProgressService _progressService;

    public bool HasUpdate { get; private set; }

    public UpdateService(ProgressService progressService)
    {
        _progressService = progressService;
    }

    public async Task<bool> CheckForUpdateAsync()
    {
        var hasUpdate = await Task.Run(async () =>
        {
            using var updateManager = new UpdateManager(_storageUrl);
            if (!updateManager.IsInstalledApp)
                return false;

            var result = await updateManager.CheckForUpdate();
            return result is not null &&
                   result.FutureReleaseEntry != result.CurrentlyInstalledVersion;
        });

        HasUpdate = hasUpdate;
        Changed?.Invoke(this, EventArgs.Empty);

        return hasUpdate;
    }

    public async Task UpdateAsync()
    {
        await _progressService.Run(async pm =>
        {
            using var mgr = new UpdateManager(_storageUrl);
            var newVersion = await mgr.UpdateApp(p => pm.Report(p, 100));
            if (newVersion is not null)
                UpdateManager.RestartApp();
        }, "Updating application");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CheckForUpdateAsync();
    }

    public event EventHandler? Changed;
}
