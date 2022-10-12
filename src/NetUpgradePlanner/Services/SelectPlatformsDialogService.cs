using System.Threading.Tasks;
using System.Windows;

using NetUpgradePlanner.Analysis;
using NetUpgradePlanner.Views;

namespace NetUpgradePlanner.Services;

internal sealed class SelectPlatformsDialogService
{
    private readonly CatalogService _catalogService;

    public SelectPlatformsDialogService(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PlatformSet?> SelectPlatformsAsync(PlatformSet platforms)
    {
        var catalog = await _catalogService.GetAsync();
        var dialog = new SelectPlatformsDialog(catalog, platforms);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() != true)
            return null;

        return dialog.Platforms;
    }
}