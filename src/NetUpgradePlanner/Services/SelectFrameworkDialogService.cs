using System.Windows;

using NetUpgradePlanner.Views;

namespace NetUpgradePlanner.Services;

internal sealed class SelectFrameworkDialogService
{
    private readonly CatalogService _catalogService;

    public SelectFrameworkDialogService(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<string?> SelectFrameworkAsync(string framework)
    {
        var catalog = await _catalogService.GetAsync();
        var dialog = new SelectFrameworkDialog(catalog, framework);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() != true)
            return null;

        return dialog.Framework;
    }
}
